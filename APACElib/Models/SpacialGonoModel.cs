﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace APACElib.Models
{
    public class SpacialGonoModel : GonoModel
    {
        enum Sites { Site1, Site2 };
        List<string> _sites = new List<string>() { "Site 1", "Site 2" };

        public SpacialGonoModel() : base()
        {

        }

        public override void BuildModel()
        {
            _specialStatInfo.Reset(_sites.Count);
            // add the parameters from the parameter sheet
            AddParameters();
            // add gono parameters 
            AddGonoParameters(_sites);
            // add classes
            AddGonoClasses(_sites);

        }


    }
}
