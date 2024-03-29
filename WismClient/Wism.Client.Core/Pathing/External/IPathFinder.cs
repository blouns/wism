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
//  Modified for use in WISM by Brian Lounsberry
//

using System.Collections.Generic;
using Wism.Client.Core;

namespace Wism.Client.Pathing.External
{
    [Author("Franco, Gustavo")]
    public interface IPathFinder
    {
        #region Events

        event PathFinderDebugHandler PathFinderDebug;

        #endregion

        #region Properties

        bool Stopped { get; }

        HeuristicFormula Formula { get; set; }

        bool Diagonals { get; set; }

        bool HeavyDiagonals { get; set; }

        int HeuristicEstimate { get; set; }

        bool PunishChangeDirection { get; set; }

        bool ReopenCloseNodes { get; set; }

        bool TieBreaker { get; set; }

        int SearchLimit { get; set; }

        double CompletedTime { get; set; }

        bool DebugProgress { get; set; }

        bool DebugFoundPath { get; set; }

        #endregion

        #region Methods

        void FindPathStop();
        List<AStarPathNode> FindPath(Tile start, Tile end);

        #endregion
    }
}