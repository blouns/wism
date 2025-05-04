// File: Wism.Client.AI/Tactical/IBid.cs

using System.Collections.Generic;
using Wism.Client.MapObjects;

namespace Wism.Client.AI.Tactical
{
    public interface IBid
    {
        /// <summary>
        /// The army that this bid applies to.
        /// </summary>
        List<Army> Armies { get; }

        /// <summary>
        /// The utility score of this bid. Higher is better.
        /// </summary>
        double Utility { get; }

        /// <summary>
        /// The tactical module that generated this bid.
        /// </summary>
        ITacticalModule Module { get; }
    }
}