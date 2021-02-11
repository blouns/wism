using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace Assets.Scripts.Editors
{
    public class LocationContainer : MonoBehaviour
    {
        [SerializeField]
        public bool ImportLocationsFromTilemap;

        [SerializeField]
        public bool Reset;

        [SerializeField]
        internal GameObject LibraryPrefab;
        [SerializeField]
        internal GameObject RuinsPrefab;
        [SerializeField]
        internal GameObject SagePrefab;
        [SerializeField]
        internal GameObject TemplePrefab;
        [SerializeField]
        internal GameObject TombPrefab;

        [SerializeField]
        public int TotalLocations;
    }
}
