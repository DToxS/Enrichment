/*
Created by Jens Hansen on 10/1/17.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Global_parameter
{
    class Global_class
    {
        public static string Empty_entry { get { return "E_m_p_t_y"; } }
        public static char Tab { get { return '\t'; } }
    }

    class Global_directory_class
    {
        public static string Hard_drive { get { return "C:\\"; } }
        public static string Base_directory { get { return Hard_drive + "LINCS_enrichment\\"; } }
        public static string Results_directory { get { return Base_directory + "Results\\"; } }
        public static string Ontology_directory { get { return Base_directory + "Ontologies\\"; } }
        public static string Degs_directory { get { return Base_directory + "DEGs\\"; } }
    }

    class Overlap_class
    {
        public static string[] Get_intersection(string[] list1, string[] list2)
        {
            list1 = list1.Distinct().OrderBy(l => l).ToArray();
            list2 = list2.Distinct().OrderBy(l => l).ToArray();
            int list1_length = list1.Length;
            int list2_length = list2.Length;
            int index1 = 0;
            int index2 = 0;
            int stringCompare;
            List<string> intersection = new List<string>();
            while ((index1 < list1_length) && (index2 < list2_length))
            {
                stringCompare = list2[index2].CompareTo(list1[index1]);
                if (stringCompare < 0) { index2++; }
                else if (stringCompare > 0) { index1++; }
                else
                {
                    intersection.Add(list1[index1]);
                    index1++;
                    index2++;
                }
            }
            return intersection.ToArray();
        }
    }

}
