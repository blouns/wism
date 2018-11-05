using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BranallyGames.Wism
{

    public class Affiliation
    {
        private AffiliationInfo info;

        private bool isHuman = false;

        public string DisplayName { get => info.DisplayName; set => info.DisplayName = value; }

        public bool IsHuman { get => isHuman; set => isHuman = value; }

        public static Affiliation Create(AffiliationInfo info)
        {
            return new Affiliation(info);
        }

        private Affiliation(AffiliationInfo info)
        {
            this.info = info;
        }

        public override string ToString()
        {
            return String.Format("{0} ({1})", 
                this.DisplayName, 
                isHuman ? "Human" : "CPU");
        }
    }
}
