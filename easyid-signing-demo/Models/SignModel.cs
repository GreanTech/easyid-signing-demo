using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace iframe_demo.Models
{
    public class SignMethod
    {
        public string Id { get; set; }
        public string DisplayName { get; set; }
    }

    public class SignModel
    {
        public SignModel()
        {
            this.SignMethods = new SignMethod[] { };
        }

        public string TextToSign { get; set; }

        public SignMethod [] SignMethods { get; set; }
    }
}