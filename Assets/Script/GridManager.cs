using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    public Tilemap tilemap; // 타일맵 참조
    public LayerMask unwalkableMask; // 'Wall' 레이어와 같은 감지할 레이어
    public Node[,] grid;
    public int gridSizeX, gridSizeY;
    public Vector3 gridBottomLeft;

    public void Awake()
    {
        SetGrid();
    }

    public void SetGrid()
    {
        // 타일맵의 경계를 가져와서 그리드 크기 계산
        BoundsInt bounds = tilemap.cellBounds;
        gridSizeX = bounds.size.x; // 타일맵의 너비
        gridSizeY = bounds.size.y; // 타일맵의 높이

        // 그리드의 왼쪽 아래 좌표 설정 (타일맵의 시작점)
        gridBottomLeft = tilemap.CellToWorld(bounds.min);

        // 그리드를 생성
        CreateGrid();
    }

    void CreateGrid()
    {
        // 그리드 크기만큼 노드 배열을 생성
        grid = new Node[gridSizeX, gridSizeY];

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                // 각 노드의 월드 좌표를 타일맵의 셀 좌표에서 변환
                Vector3Int cellPosition = new Vector3Int(x + tilemap.cellBounds.min.x, y + tilemap.cellBounds.min.y, 0);
                Vector3 worldPoint = tilemap.CellToWorld(cellPosition) + new Vector3(tilemap.cellSize.x / 2, tilemap.cellSize.y / 2, 0);

                // 해당 좌표에 'Wall' 레이어에 해당하는 Collider2D가 있는지 검사
                //bool walkable = !Physics2D.OverlapPoint(worldPoint, unwalkableMask); // 해당 좌표에 충돌체가 있는지 확인
                bool walkable = !Physics2D.OverlapBox(worldPoint,
    new Vector2(tilemap.cellSize.x * 0.5f, tilemap.cellSize.y * 0.5f),
    0f, unwalkableMask);
                grid[x, y] = new Node(worldPoint, walkable, x, y);
            }
        }
    }

    public Node NodeFromWorldPoint(Vector2 worldPosition)
    {
        // 월드 좌표를 그리드 좌표로 변환
        float percentX = (worldPosition.x - gridBottomLeft.x) / (gridSizeX * tilemap.cellSize.x);
        float percentY = (worldPosition.y - gridBottomLeft.y) / (gridSizeY * tilemap.cellSize.y);
        percentX = Mathf.Clamp01(percentX);
        percentY = Mathf.Clamp01(percentY);

        int x = Mathf.RoundToInt((gridSizeX - 1) * percentX);
        int y = Mathf.RoundToInt((gridSizeY - 1) * percentY);
        return grid[x, y];
    }

    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                    continue;

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX >= 0 && checkX < gridSizeX && checkY >= 0 && checkY < gridSizeY)
                {
                    neighbours.Add(grid[checkX, checkY]);
                }
            }
        }

        return neighbours;
    }
}