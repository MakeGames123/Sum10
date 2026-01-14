using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PathFinder
{
    float minSmartDis = 200;
    int size;
    readonly Vector2Int[] dirs =
   {
        Vector2Int.up,
        Vector2Int.down,
        Vector2Int.left,
        Vector2Int.right
    };
    public void SetSize(int n)
    {
        size = n;
    }
    public bool HasAnyValidPath(List<Cell> cells, int target = 10)
    {
        List<Cell> path = FindPathBFS(cells, target);
        /*
        if(path != null)
        {
            foreach(Cell cell in path)
            {
                Debug.Log(cell.Position);
            }
        }*/

        return path != null;
    }

    public List<Cell> FindPathBFS(List<Cell> cells, int target)
    {
        // 시작점이 될 수 있는 모든 셀
        var startPoints = cells.Where(c => c.ReturnNum() != 0).ToList();

        foreach (var startCell in startPoints)
        {
            Queue<PathState> queue = new Queue<PathState>();
            bool[,] visitedInThisRun = new bool[size, size];

            int startVal = startCell.ReturnNum();
            int initialSum = startVal > 0 ? startVal : 0;

            // 시작 상태 생성
            queue.Enqueue(new PathState(startCell.Position.x, startCell.Position.y, initialSum, new List<Cell>(), startCell));
            visitedInThisRun[startCell.Position.x, startCell.Position.y] = true;

            while (queue.Count > 0)
            {
                PathState current = queue.Dequeue();

                // 1. 규칙 체크 (성공 시 해당 경로 즉시 반환)
                int actualVal = cells[current.x + current.y * size].ReturnNum();

                if (current.currentSum == target || actualVal < 0)
                {
                    return current.path;
                }

                // 3. 인접 칸 탐색
                foreach (var d in dirs)
                {
                    int nx = current.x + d.x;
                    int ny = current.y + d.y;

                    if (IsInside(nx, ny) && !visitedInThisRun[nx, ny])
                    {
                        Cell nextCell = cells[nx + ny * size];
                        int nextVal = nextCell.ReturnNum();
                        int addedVal = nextVal > 0 ? nextVal : 0;

                        if (current.currentSum + addedVal <= target && nextCell.ReturnNum() != -2)
                        {
                            visitedInThisRun[nx, ny] = true;
                            queue.Enqueue(new PathState(nx, ny, current.currentSum + addedVal, current.path, nextCell));
                        }
                    }
                }
            }
        }

        return null; // 모든 시작점에서 찾아봤으나 경로가 없는 경우
    }
    public Cell FindNearestCellToScreenPoint(Vector2 screenPos, List<CellSlot> cellSlots)
    {
        CellSlot nearest = null;
        float min = float.MaxValue;
        for (int i = 0; i < size * size; i++)
        {
            var cell = cellSlots[i];

            var cellRect = cell.GetComponent<RectTransform>();

            if (cellRect == null || cell.cellInfo == null)
            {
                continue;
            }

            Vector2 cellScreenPos = RectTransformUtility.WorldToScreenPoint(null, cellRect.position);
            float dist = Vector2.Distance(screenPos, cellScreenPos);

            if (dist < min && dist < minSmartDis)
            {
                min = dist;
                nearest = cell;
            }
        }

        if (nearest == null)
        {
            return null;
        }

        return nearest.cellInfo;
    }

    public List<Cell> FindPathBetweenCells(Cell from, Cell to, List<Cell> selectedCells, List<Cell> cells)
    {
        var path = new List<Cell>();

        if (from == null)//맨 처음 클릭 처리용
        {
            path.Add(to);
            return path;
        }

        int currentX = from.Position.x;
        int currentY = from.Position.y;
        int targetX = to.Position.x;
        int targetY = to.Position.y;

        while (currentX != targetX || currentY != targetY)
        {
            if (currentX != targetX)
                currentX += targetX > currentX ? 1 : -1;
            else if (currentY != targetY)
                currentY += targetY > currentY ? 1 : -1;

            if (!IsInside(currentX, currentY))
                return null;

            var cell = cells[currentX + currentY * size];

            if (selectedCells.Contains(cell))
                continue;

            path.Add(cell);
        }

        return path;
    }

    private bool IsInside(int x, int y)
    {
        return x >= 0 && x < size && y >= 0 && y < size;
    }

    private class PathState
    {
        public int x, y;
        public int currentSum;
        public List<Cell> path;

        public PathState(int x, int y, int sum, List<Cell> prevPath, Cell currentCell)
        {
            this.x = x;
            this.y = y;
            this.currentSum = sum;
            this.path = new List<Cell>(prevPath) { currentCell };
        }
    }
}