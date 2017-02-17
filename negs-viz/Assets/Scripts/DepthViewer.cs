using UnityEngine;

public class SubMesh
{
    public GameObject gameObject;

    private Mesh _Mesh;
    public Vector3[] _Vertices;
    private Vector2[] _UV;
    private int[] _Triangles;

    private Texture2D _texture;
    private int _width;
    private int _height;
    public Color[] colors;

    public SubMesh(int width, int height)
    {
        _width = width;
        _height = height;

        gameObject = new GameObject();
        gameObject.AddComponent<MeshFilter>();
        gameObject.AddComponent<MeshRenderer>().material = Resources.Load("Materials/MaterialLouco") as Material;

        CreateMesh(width, height);

        _texture = new Texture2D(_width, _height);
        colors = new Color[_width * _height];
    }

    void CreateMesh(int width, int height)
    {
        _Mesh = new Mesh();
        gameObject.GetComponent<MeshFilter>().mesh = _Mesh;

        _Vertices = new Vector3[width * height];
        _UV = new Vector2[width * height];
        _Triangles = new int[6 * ((width - 1) * (height - 1))];

        int triangleIndex = 0;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = (y * width) + x;

                _Vertices[index] = new Vector3(x, -y, 0);
                _UV[index] = new Vector2(((float)x / (float)width), ((float)y / (float)height));

                // Skip the last row/col
                if (x != (width - 1) && y != (height - 1))
                {
                    int topLeft = index;
                    int topRight = topLeft + 1;
                    int bottomLeft = topLeft + width;
                    int bottomRight = bottomLeft + 1;

                    _Triangles[triangleIndex++] = topLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomLeft;
                    _Triangles[triangleIndex++] = bottomLeft;
                    _Triangles[triangleIndex++] = topRight;
                    _Triangles[triangleIndex++] = bottomRight;
                }
            }
        }

        _Mesh.vertices = _Vertices;
        _Mesh.uv = _UV;
        _Mesh.triangles = _Triangles;
        _Mesh.RecalculateNormals();
    }

    public void Apply()
    {
        _texture.SetPixels(colors);
        _texture.Apply();

        gameObject.GetComponent<Renderer>().material.mainTexture = _texture;

        _Mesh.vertices = _Vertices;
        //_Mesh.uv
        //_Mesh.triangles = _Triangles;
        //_Mesh.RecalculateNormals();
    }
}

public class DepthViewer : MonoBehaviour
{

    private SubMesh[] subMeshes;

    public Color[] colors;
    public ushort[] depth;

    //[Range(0,1)]
    private float depthScale = 0.3f;

    private int _width;
    private int _height;

    void Start()
    {
        _width = Properties.Instance.FrameDescription_Width;
        _height = Properties.Instance.FrameDescription_Height;

        subMeshes = new SubMesh[4];
        for (int i = 0; i < subMeshes.Length; i++)
        {
            SubMesh subMesh = new SubMesh(_width, _height / subMeshes.Length);
            Vector3 position = subMesh.gameObject.transform.position;
            position.y = -(i * (_height / (float)subMeshes.Length - 1.0f));
            subMesh.gameObject.name = "SubMesh" + i;
            subMesh.gameObject.transform.position = position;
            subMesh.gameObject.transform.parent = gameObject.transform;
            subMeshes[i] = subMesh;
        }

        gameObject.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
    }

    void Update()
    {
        int submeshLength = colors.Length / subMeshes.Length;
        int a, b;
        for (int i = 0; i < colors.Length; i++)
        {
            a = i / submeshLength;
            b = i % submeshLength;

            subMeshes[a].colors[b] = colors[i];
            subMeshes[a]._Vertices[b].z = (depth[i] == 0 || depth[1] > 1500) ? 1000000 : depth[i] * (float)depthScale;
        }

        foreach(SubMesh m in subMeshes)
        {
            m.Apply();
            m.gameObject.GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();
        }        
    }
}
