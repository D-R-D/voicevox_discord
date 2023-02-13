using Discord;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace voicevox_discord
{
    internal class JsonEditor
    {
        public static Dictionary<string,object> ObjectFromJson(string json)
        {
            if (Tools.IsNullOrEmpty(json)) { throw new ArgumentNullException(nameof(json)); }

            ArrayList speakers = JsonConvert.DeserializeObject<ArrayList>(json)!;

            Dictionary<string,object> speakerobj = new Dictionary<string,object>();

            foreach(var speaker in speakers)
            {
                Dictionary<string, object> speakerobject = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(speaker))!;
                ArrayList stylelist = JsonConvert.DeserializeObject<ArrayList>(JsonConvert.SerializeObject(speakerobject["styles"]))!;

                Dictionary<string, string> styleobject = new Dictionary<string, string>();
                foreach(var styles in stylelist)
                {
                    Dictionary<string, object> style = JsonConvert.DeserializeObject<Dictionary<string, object>>(JsonConvert.SerializeObject(styles))!;
                    styleobject.Add(style["name"].ToString()!, style["id"].ToString()!);
                }

                speakerobj.Add(speakerobject["name"].ToString()!, styleobject);
            }

            return speakerobj;
        }

        public static List<Dictionary<string,object>> CreatePagedObject(Dictionary<string,object> objects,int pagecount)
        {
            List<Dictionary<string, object>> pagedobjects = new();

            int i = 1;
            Dictionary<string, object> tmpobjects = new();
            foreach (KeyValuePair<string, object> speaker in objects)
            {
                if (i > pagecount)
                {
                    pagedobjects.Add(new Dictionary<string, object>(tmpobjects));
                    tmpobjects.Clear();
                    i = 0;
                }

                tmpobjects.Add(speaker.Key, speaker.Value);
                i++;
            }
            pagedobjects.Add(tmpobjects);

            return pagedobjects;
        }
    }
}
