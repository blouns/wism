//
//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER 
//  REMAINS UNCHANGED.
//
//  Email:  gustavo_franco@hotmail.com
//
//  Copyright (C) 2006 Franco, Gustavo 
//
//
//  Modified for use in WISM by Brian Lounsberry
//

#define DEBUGON

using System;
using System.Collections.Generic;
using System.Drawing;
using Wism.Client.Core;
using Wism.Client.MapObjects;

namespace Wism.Client.Pathing.External
{
    #region Structs

    [Author("Franco, Gustavo")]
    public struct PathFinderNode
    {
        #region Variables Declaration

        public int F;
        public int G;
        public int H; // f = gone + heuristic
        public int X;
        public int Y;
        public int PX; // Parent
        public int PY;

        #endregion
    }

    #endregion

    #region Enum

    [Author("Franco, Gustavo")]
    public enum PathFinderNodeType
    {
        Start = 1,
        End = 2,
        Open = 4,
        Close = 8,
        Current = 16,
        Path = 32
    }

    public enum HeuristicFormula
    {
        Manhattan = 1,
        MaxDXDY = 2,
        DiagonalShortCut = 3,
        Euclidean = 4,
        EuclideanNoSQR = 5,
        Custom1 = 6
    }

    #endregion

    #region Delegates

    public delegate void PathFinderDebugHandler(int fromX, int fromY, int x, int y, PathFinderNodeType type,
        int totalCost, int cost);

    #endregion

    [Author("Franco, Gustavo")]
    public class PathFinder : IPathFinder
    {
        #region Constructors

        public PathFinder(Tile[,] grid, List<Army> armiesToMove)
        {
            this.mGrid = grid ?? throw new ArgumentNullException(nameof(grid));
            this.mArmiesToMove = armiesToMove ?? throw new ArgumentNullException(nameof(armiesToMove));
        }

        #endregion

        #region Events

        public event PathFinderDebugHandler PathFinderDebug;

        #endregion

        #region Inner Classes

        [Author("Franco, Gustavo")]
        internal class ComparePFNode : IComparer<AStarPathNode>
        {
            #region IComparer Members

            public int Compare(AStarPathNode x, AStarPathNode y)
            {
                if (x.F > y.F)
                {
                    return 1;
                }

                if (x.F < y.F)
                {
                    return -1;
                }

                return 0;
            }

            #endregion
        }

        #endregion

        #region Variables Declaration

        private readonly List<Army> mArmiesToMove;
        private readonly Tile[,] mGrid;
        private readonly PriorityQueueB<AStarPathNode> mOpen = new PriorityQueueB<AStarPathNode>(new ComparePFNode());
        private readonly List<AStarPathNode> mClose = new List<AStarPathNode>();
        private bool mStop;
        private int mHoriz;

        #endregion

        #region Properties

        public bool Stopped { get; private set; } = true;

        public HeuristicFormula Formula { get; set; } = HeuristicFormula.Manhattan;

        public bool Diagonals { get; set; } = true;

        public bool HeavyDiagonals { get; set; } = false;

        public int HeuristicEstimate { get; set; } = 2;

        public bool PunishChangeDirection { get; set; } = false;

        public bool ReopenCloseNodes { get; set; } = false;

        public bool TieBreaker { get; set; } = false;

        public int SearchLimit { get; set; } = 100000;

        public double CompletedTime { get; set; } = 0;

        public bool DebugProgress { get; set; } = false;

        public bool DebugFoundPath { get; set; } = false;

        public bool IgnoreClan { get; set; } = false;

        #endregion

        #region Methods

        public void FindPathStop()
        {
            this.mStop = true;
        }

        public List<AStarPathNode> FindPath(Tile start, Tile end)
        {
            //HighResolutionTime.Start();

            AStarPathNode parentNode;
            var found = false;
            var gridX = this.mGrid.GetUpperBound(0) + 1;
            var gridY = this.mGrid.GetUpperBound(1) + 1;

            this.mStop = false;
            this.Stopped = false;
            this.mOpen.Clear();
            this.mClose.Clear();

#if DEBUGON
            if (this.DebugProgress && this.PathFinderDebug != null)
            {
                this.PathFinderDebug(0, 0, start.X, start.Y, PathFinderNodeType.Start, -1, -1);
            }

            if (this.DebugProgress && this.PathFinderDebug != null)
            {
                this.PathFinderDebug(0, 0, end.X, end.Y, PathFinderNodeType.End, -1, -1);
            }
#endif

            sbyte[,] direction;
            if (this.Diagonals)
            {
                direction = new sbyte[8, 2]
                    { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 }, { 1, -1 }, { 1, 1 }, { -1, 1 }, { -1, -1 } };
            }
            else
            {
                direction = new sbyte[4, 2] { { 0, -1 }, { 1, 0 }, { 0, 1 }, { -1, 0 } };
            }

            parentNode = new AStarPathNode();
            parentNode.G = 0;
            parentNode.H = this.HeuristicEstimate;
            parentNode.F = parentNode.G + parentNode.H;
            parentNode.Value = start;
            parentNode.Previous = parentNode;
            this.mOpen.Push(parentNode);
            while (this.mOpen.Count > 0 && !this.mStop)
            {
                parentNode = this.mOpen.Pop();

#if DEBUGON
                if (this.DebugProgress && this.PathFinderDebug != null)
                {
                    this.PathFinderDebug(0, 0, parentNode.X, parentNode.Y, PathFinderNodeType.Current, -1, -1);
                }
#endif

                if (parentNode.X == end.X && parentNode.Y == end.Y)
                {
                    this.mClose.Add(parentNode);
                    found = true;
                    break;
                }

                if (this.mClose.Count > this.SearchLimit)
                {
                    this.Stopped = true;
                    return null;
                }

                if (this.PunishChangeDirection)
                {
                    this.mHoriz = parentNode.X - parentNode.PX;
                }

                //Lets calculate each successors
                for (var i = 0; i < (this.Diagonals ? 8 : 4); i++)
                {
                    var newNode = new AStarPathNode();
                    var newX = parentNode.X + direction[i, 0];
                    var newY = parentNode.Y + direction[i, 1];

                    // Is this within our map?
                    if (newX < 0 || newY < 0 || newX >= gridX || newY >= gridY)
                    {
                        continue;
                    }

                    // Is this traversable?
                    if (!this.mGrid[newX, newY].CanTraverseHere(this.mArmiesToMove, this.IgnoreClan))
                    {
                        continue;
                    }

                    newNode.Value = this.mGrid[newX, newY];

                    int newG;
                    if (this.HeavyDiagonals && i > 3)
                    {
                        newG = parentNode.G + (int)(newNode.GetMovementCost(this.mArmiesToMove) * 2.41f);
                    }
                    else
                    {
                        newG = parentNode.G + newNode.GetMovementCost(this.mArmiesToMove);
                    }


                    if (newG == parentNode.G)
                    {
                        //Unbrekeable
                        continue;
                    }

                    if (this.PunishChangeDirection)
                    {
                        if (newNode.X - parentNode.X != 0)
                        {
                            if (this.mHoriz == 0)
                            {
                                newG += 20;
                            }
                        }

                        if (newNode.Y - parentNode.Y != 0)
                        {
                            if (this.mHoriz != 0)
                            {
                                newG += 20;
                            }
                        }
                    }

                    var foundInOpenIndex = -1;
                    for (var j = 0; j < this.mOpen.Count; j++)
                    {
                        if (this.mOpen[j].X == newNode.X && this.mOpen[j].Y == newNode.Y)
                        {
                            foundInOpenIndex = j;
                            break;
                        }
                    }

                    if (foundInOpenIndex != -1 && this.mOpen[foundInOpenIndex].G <= newG)
                    {
                        continue;
                    }

                    var foundInCloseIndex = -1;
                    for (var j = 0; j < this.mClose.Count; j++)
                    {
                        if (this.mClose[j].X == newNode.X && this.mClose[j].Y == newNode.Y)
                        {
                            foundInCloseIndex = j;
                            break;
                        }
                    }

                    if (foundInCloseIndex != -1 && (this.ReopenCloseNodes || this.mClose[foundInCloseIndex].G <= newG))
                    {
                        continue;
                    }

                    newNode.Previous = parentNode;
                    newNode.G = newG;

                    switch (this.Formula)
                    {
                        default:
                        case HeuristicFormula.Manhattan:
                            newNode.H = this.HeuristicEstimate *
                                        (Math.Abs(newNode.X - end.X) + Math.Abs(newNode.Y - end.Y));
                            break;
                        case HeuristicFormula.MaxDXDY:
                            newNode.H = this.HeuristicEstimate *
                                        Math.Max(Math.Abs(newNode.X - end.X), Math.Abs(newNode.Y - end.Y));
                            break;
                        case HeuristicFormula.DiagonalShortCut:
                            var h_diagonal = Math.Min(Math.Abs(newNode.X - end.X), Math.Abs(newNode.Y - end.Y));
                            var h_straight = Math.Abs(newNode.X - end.X) + Math.Abs(newNode.Y - end.Y);
                            newNode.H = this.HeuristicEstimate * 2 * h_diagonal +
                                        this.HeuristicEstimate * (h_straight - 2 * h_diagonal);
                            break;
                        case HeuristicFormula.Euclidean:
                            newNode.H = (int)(this.HeuristicEstimate *
                                              Math.Sqrt(Math.Pow(newNode.X - end.X, 2) +
                                                        Math.Pow(newNode.Y - end.Y, 2)));
                            break;
                        case HeuristicFormula.EuclideanNoSQR:
                            newNode.H = (int)(this.HeuristicEstimate *
                                              (Math.Pow(newNode.X - end.X, 2) + Math.Pow(newNode.Y - end.Y, 2)));
                            break;
                        case HeuristicFormula.Custom1:
                            var dxy = new Point(Math.Abs(end.X - newNode.X), Math.Abs(end.Y - newNode.Y));
                            var Orthogonal = Math.Abs(dxy.X - dxy.Y);
                            var Diagonal = Math.Abs((dxy.X + dxy.Y - Orthogonal) / 2);
                            newNode.H = this.HeuristicEstimate * (Diagonal + Orthogonal + dxy.X + dxy.Y);
                            break;
                    }

                    if (this.TieBreaker)
                    {
                        var dx1 = parentNode.X - end.X;
                        var dy1 = parentNode.Y - end.Y;
                        var dx2 = start.X - end.X;
                        var dy2 = start.Y - end.Y;
                        var cross = Math.Abs(dx1 * dy2 - dx2 * dy1);
                        newNode.H = (int)(newNode.H + cross * 0.001);
                    }

                    newNode.F = newNode.G + newNode.H;

#if DEBUGON
                    if (this.DebugProgress && this.PathFinderDebug != null)
                    {
                        this.PathFinderDebug(parentNode.X, parentNode.Y, newNode.X, newNode.Y, PathFinderNodeType.Open,
                            newNode.F, newNode.G);
                    }
#endif


                    //It is faster if we leave the open node in the priority queue
                    //When it is removed, all nodes around will be closed, it will be ignored automatically
                    //if (foundInOpenIndex != -1)
                    //    mOpen.RemoveAt(foundInOpenIndex);

                    //if (foundInOpenIndex == -1)
                    this.mOpen.Push(newNode);
                }

                this.mClose.Add(parentNode);

#if DEBUGON
                if (this.DebugProgress && this.PathFinderDebug != null)
                {
                    this.PathFinderDebug(0, 0, parentNode.X, parentNode.Y, PathFinderNodeType.Close, parentNode.F,
                        parentNode.G);
                }
#endif
            }

            //mCompletedTime = HighResolutionTime.GetTime();
            if (found)
            {
                var fNode = this.mClose[this.mClose.Count - 1];
                for (var i = this.mClose.Count - 1; i >= 0; i--)
                {
                    if ((fNode.PX == this.mClose[i].X && fNode.PY == this.mClose[i].Y) || i == this.mClose.Count - 1)
                    {
#if DEBUGON
                        if (this.DebugFoundPath && this.PathFinderDebug != null)
                        {
                            this.PathFinderDebug(fNode.X, fNode.Y, this.mClose[i].X, this.mClose[i].Y,
                                PathFinderNodeType.Path, this.mClose[i].F, this.mClose[i].G);
                        }
#endif
                        fNode = this.mClose[i];
                    }
                    else
                    {
                        this.mClose.RemoveAt(i);
                    }
                }

                this.Stopped = true;
                return this.mClose;
            }

            this.Stopped = true;
            return null;
        }

        #endregion
    }
}