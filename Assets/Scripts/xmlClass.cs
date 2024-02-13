using System.Collections.Generic;
using System.Xml;
using System.Xml.Linq;
using System.Linq;
using UnityEngine;

namespace xmlClass
{
    public class xmlDoc{

        public XDocument doc;
        public List<seamClass> seamList = new List<seamClass>();
        
        public xmlDoc(string xmlPath)
        {
            doc = XDocument.Parse(Resources.Load<TextAsset>(xmlPath).text);
            int numOfSeams = 0;
            foreach (XElement el in doc.Descendants("SNaht"))
            {
                seamList.Add(new seamClass(el, numOfSeams));
                numOfSeams++;
            }
        }
        
    }

    public class seamClass
    {
        public int index;
        public string name;
        public int ZRotLock;
        public int WkzWkl;
        public string WkzName;

        public List<pointClass> pointList = new List<pointClass>();

        public List<frameClass> frameList = new List<frameClass>();
        
        public seamClass(XElement doc, int index)
        {
            this.index = index;
            name = doc.Attribute("Name").Value;
            WkzName = doc.Attribute("WkzName").Value;
            ZRotLock = int.Parse(doc.Attribute("ZRotLock").Value);
            WkzWkl = int.Parse(doc.Attribute("WkzWkl").Value);
            foreach (XElement el in doc.Descendants("Punkt")){
                pointList.Add(new pointClass(el));
            }
            foreach (XElement el in doc.Descendants("Frame")){
                frameList.Add(new frameClass(el));
            }
        }
    }

    public class pointClass
    {
        public Vector3 pos;
        public Vector3 plane1;
        public Vector3 plane2;
        
        public pointClass(XElement punkt)
        {
            pos = parseVector(punkt);
            List<XElement> temp_list = punkt.Descendants().ToList();
            plane1 = parseVector(temp_list[0]);
            plane2 = parseVector(temp_list[1]);
        }

        public Vector3 parseVector(XElement element)
        {
            return new Vector3(float.Parse(element.Attribute("X").Value), float.Parse(element.Attribute("Y").Value), float.Parse(element.Attribute("Z").Value));
        }
    }

    public class frameClass
    {
        public Vector3 pos;
        public Vector3 XVek;
        public Vector3 YVek;
        public Vector3 ZVek;
        
        public frameClass(XElement frame)
        {
            pos = parseVector(frame.Descendants("Pos").First());
            XVek = parseVector(frame.Descendants("XVek").First());
            YVek = parseVector(frame.Descendants("YVek").First());
            ZVek = parseVector(frame.Descendants("ZVek").First());
        }
        
        public Vector3 parseVector(XElement element)
        {
            return new Vector3(float.Parse(element.Attribute("X").Value), float.Parse(element.Attribute("Y").Value), float.Parse(element.Attribute("Z").Value));
        } 
    }

    

}

