using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoinRush.Data;
using UnityEngine;

namespace CoinRush.Level
{
    /// <summary>
    /// A procedurally generated low-poly tree, grown at runtime from a stochastic 3D L-system.
    /// <para>
    /// Alphabet: F = draw a branch segment, X = growth bud (becomes L when the iterations run out),
    /// L = leaf cluster, + / - = tilt around the turtle's Z axis, &amp; / ^ = pitch around its X axis,
    /// / and \ = roll by the golden angle (137.5°) so branches spiral around the trunk, [ ] = push / pop
    /// the turtle (start / end a branch). Every [ makes branches shorter (lengthScale) and thinner.
    /// </para>
    /// The same seed always grows the same tree. When the tree appears it animates from a seedling to
    /// full size.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class FractalTree : MonoBehaviour
    {
        private const string Axiom = "FX";
        private const float GoldenAngle = 137.5f;
        private const float RadiusScale = 0.62f;
        private const int Sides = 6;

        // Stochastic production rules for the bud X: (replacement, probability).
        private static readonly (string rule, float p)[] BudRules =
        {
            ("F[&X]/[&X]/[&X]", 0.45f),   // three-way split, spiralling
            ("F[+X][-X]/F[&X]L", 0.30f),  // fork, then a side shoot with leaves
            ("F/[&X]//[^X]FX", 0.25f),    // long leader with alternating side branches
        };

        [SerializeField, Min(0.1f)] private float growTime = 1.6f;

        private float growStart, growDelay;
        private bool growing;

        /// <summary>Grows this tree's mesh. Called by <see cref="FractalForest"/>.</summary>
        public void Generate(ForestData settings, int seed, Material bark, Material leaves)
        {
            var rng = new System.Random(seed);
            string program = Expand(Axiom, Mathf.Clamp(settings.iterations, 1, 6), rng);
            Mesh mesh = BuildMesh(program, settings, rng, out float trunkHeight);
            mesh.name = $"FractalTree_{seed}";

            GetComponent<MeshFilter>().sharedMesh = mesh;
            GetComponent<MeshRenderer>().sharedMaterials = new[] { bark, leaves };

            // Only the trunk is solid; the crown is decoration you can walk under.
            CapsuleCollider trunk = gameObject.AddComponent<CapsuleCollider>();
            trunk.radius = settings.trunkRadius * 1.2f;
            trunk.height = trunkHeight;
            trunk.center = new Vector3(0f, trunkHeight * 0.5f, 0f);

            growDelay = (float)rng.NextDouble() * 0.6f;
            growStart = Time.time;
            growing = true;
            transform.localScale = Vector3.one * 0.05f;
        }

        private void Update()
        {
            if (!growing) return;
            float t = Mathf.Clamp01((Time.time - growStart - growDelay) / growTime);
            float eased = t * t * (3f - 2f * t); // smoothstep
            transform.localScale = Vector3.one * Mathf.Lerp(0.05f, 1f, eased);
            if (t >= 1f) growing = false;
        }

        // ------------------------------------------------------------------ L-system

        /// <summary>Applies the production rules <paramref name="iterations"/> times.</summary>
        public static string Expand(string axiom, int iterations, System.Random rng)
        {
            string current = axiom;
            for (int i = 0; i < iterations; i++)
            {
                var next = new StringBuilder(current.Length * 4);
                foreach (char c in current) next.Append(c == 'X' ? PickRule(rng) : c.ToString());
                current = next.ToString();
            }
            return current.Replace('X', 'L'); // unfinished buds become leaf clusters
        }

        private static string PickRule(System.Random rng)
        {
            float roll = (float)rng.NextDouble(), sum = 0f;
            foreach (var (rule, p) in BudRules)
            {
                sum += p;
                if (roll <= sum) return rule;
            }
            return BudRules[0].rule;
        }

        // ------------------------------------------------------------------ turtle -> mesh

        private struct Turtle
        {
            public Vector3 position;
            public Quaternion rotation; // heading = rotation * up
            public int depth;
        }

        private static Mesh BuildMesh(string program, ForestData s, System.Random rng, out float trunkHeight)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var barkTris = new List<int>();
            var leafTris = new List<int>();
            var stack = new Stack<Turtle>();
            var turtle = new Turtle { position = Vector3.zero, rotation = Quaternion.identity, depth = 0 };
            trunkHeight = 0f;

            float Jitter(float angle) => angle * (0.8f + 0.4f * (float)rng.NextDouble());

            foreach (char c in program)
            {
                switch (c)
                {
                    case 'F':
                    {
                        float length = s.trunkLength * Mathf.Pow(s.lengthScale, turtle.depth);
                        float r0 = s.trunkRadius * Mathf.Pow(RadiusScale, turtle.depth);
                        Vector3 dir = turtle.rotation * Vector3.up;
                        Vector3 end = turtle.position + dir * length;
                        AddSegment(verts, normals, barkTris, turtle.position, end, r0, r0 * 0.82f);
                        if (turtle.depth == 0) trunkHeight = Mathf.Max(trunkHeight, end.y);
                        turtle.position = end;
                        // Gentle phototropism: branches bend slightly back towards the sky.
                        turtle.rotation = Quaternion.Slerp(turtle.rotation, Quaternion.identity, 0.08f);
                        break;
                    }
                    case 'L':
                        AddLeafCluster(verts, normals, leafTris, turtle.position, s.leafSize * (0.75f + 0.5f * (float)rng.NextDouble()), rng);
                        break;
                    case '+': turtle.rotation *= Quaternion.AngleAxis(Jitter(s.branchAngle), Vector3.forward); break;
                    case '-': turtle.rotation *= Quaternion.AngleAxis(-Jitter(s.branchAngle), Vector3.forward); break;
                    case '&': turtle.rotation *= Quaternion.AngleAxis(Jitter(s.branchAngle), Vector3.right); break;
                    case '^': turtle.rotation *= Quaternion.AngleAxis(-Jitter(s.branchAngle), Vector3.right); break;
                    case '/': turtle.rotation *= Quaternion.AngleAxis(Jitter(GoldenAngle), Vector3.up); break;
                    case '\\': turtle.rotation *= Quaternion.AngleAxis(-Jitter(GoldenAngle), Vector3.up); break;
                    case '[':
                        stack.Push(turtle);
                        turtle.depth++;
                        break;
                    case ']':
                        if (stack.Count > 0) turtle = stack.Pop();
                        break;
                }
            }

            var mesh = new Mesh();
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(barkTris, 0);
            mesh.SetTriangles(leafTris, 1);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>A tapered six-sided prism from a to b (flat-shaded for the low-poly look).</summary>
        private static void AddSegment(List<Vector3> v, List<Vector3> n, List<int> tris, Vector3 a, Vector3 b, float ra, float rb)
        {
            Vector3 axis = (b - a).normalized;
            Vector3 side = Vector3.Cross(axis, Mathf.Abs(axis.y) < 0.95f ? Vector3.up : Vector3.right).normalized;
            Vector3 other = Vector3.Cross(axis, side);
            Vector3 Ring(float radius, int i)
            {
                float ang = i * Mathf.PI * 2f / Sides;
                return (side * Mathf.Cos(ang) + other * Mathf.Sin(ang)) * radius;
            }
            for (int i = 0; i < Sides; i++)
            {
                Vector3 p0 = a + Ring(ra, i), p1 = a + Ring(ra, i + 1), p2 = b + Ring(rb, i + 1), p3 = b + Ring(rb, i);
                Vector3 normal = Vector3.Cross(p1 - p0, p3 - p0).normalized; // outward
                AddQuad(v, n, tris, p0, p1, p2, p3, normal);
            }
        }

        /// <summary>Two overlapping, randomly rotated octahedra: a chunky low-poly leaf blob.</summary>
        private static void AddLeafCluster(List<Vector3> v, List<Vector3> n, List<int> tris, Vector3 centre, float size, System.Random rng)
        {
            Vector3[] octa = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };
            int[,] faces = { { 0, 4, 3 }, { 0, 3, 5 }, { 0, 5, 2 }, { 0, 2, 4 }, { 1, 3, 4 }, { 1, 5, 3 }, { 1, 2, 5 }, { 1, 4, 2 } };
            for (int blob = 0; blob < 2; blob++)
            {
                Quaternion rot = Quaternion.Euler((float)rng.NextDouble() * 360f, (float)rng.NextDouble() * 360f, 0f);
                Vector3 offset = new Vector3((float)rng.NextDouble() - 0.5f, (float)rng.NextDouble() * 0.4f, (float)rng.NextDouble() - 0.5f) * size * 0.6f;
                Vector3 scale = new Vector3(1f, 0.75f, 1f) * size * (blob == 0 ? 1f : 0.7f);
                Vector3[] p = octa.Select(o => centre + offset + rot * Vector3.Scale(o, scale)).ToArray();
                for (int f = 0; f < 8; f++)
                {
                    Vector3 a = p[faces[f, 0]], b = p[faces[f, 1]], c = p[faces[f, 2]];
                    Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
                    int start = v.Count;
                    v.Add(a); v.Add(b); v.Add(c);
                    n.Add(normal); n.Add(normal); n.Add(normal);
                    tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
                }
            }
        }

        private static void AddQuad(List<Vector3> v, List<Vector3> n, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 normal)
        {
            int start = v.Count;
            v.Add(a); v.Add(b); v.Add(c); v.Add(d);
            for (int i = 0; i < 4; i++) n.Add(normal);
            tris.Add(start); tris.Add(start + 1); tris.Add(start + 2);
            tris.Add(start); tris.Add(start + 2); tris.Add(start + 3);
        }
    }
}
