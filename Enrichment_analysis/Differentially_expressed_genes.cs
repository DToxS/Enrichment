/*
Created by Jens Hansen on 10/1/17.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using ReadWrite;
using Global_parameter;

namespace Differentially_expressed_genes
{
    class Deg_line_class
    {
        #region Fields
        public string Sequencing_run {get;set;}
        public string Cell { get; set; }
        public string Condition1 { get; set; }
        public string Condition2 { get; set; }
        public string Gene { get; set; }
        public float logFC { get; set; }
        public float logCPM { get; set; }
        public float PValue { get; set; }
        #endregion

        public Deg_line_class()
        {
            this.Sequencing_run = "";
            this.Cell = "";
            this.Condition1 = "";
            this.Condition2 = "";
            this.Gene = "";
        }

        public Deg_line_class Deep_copy()
        {
            Deg_line_class copy = (Deg_line_class)this.MemberwiseClone();
            copy.Cell = (string)this.Cell.Clone();
            copy.Condition1 = (string)this.Condition1.Clone();
            copy.Condition2 = (string)this.Condition2.Clone();
            copy.Gene = (string)this.Gene.Clone();
            return copy;
        }
    }

    class Deg_readOptions_class : ReadWriteOptions_base
    {
        public Deg_readOptions_class(string complete_file_name)
        {
            File = complete_file_name;
            Key_propertyNames = new string[] { "Gene", "Cell", "Condition1","Condition2","Gene","logFC", "logCPM", "PValue" };
            Key_columnNames = Key_propertyNames;
            HeadlineDelimiters = new char[] { Global_class.Tab };
            LineDelimiters = new char[] { Global_class.Tab };
            File_has_headline = true;
            Report = ReadWrite_report_enum.Report_main;
        }
    }

    class Deg_class
    {
        public Deg_line_class[] Degs { get; set; }

        public Deg_class()
        {
            this.Degs = new Deg_line_class[0];
        }

        #region Generate
        private void Add_to_array(Deg_line_class[] add_degs)
        {
            int this_length = this.Degs.Length;
            int add_length = add_degs.Length;
            int new_length = this_length + add_length;
            int indexNew = -1;
            Deg_line_class[] new_degs = new Deg_line_class[new_length];
            for (int indexThis=0; indexThis<this_length; indexThis++)
            {
                indexNew++;
                new_degs[indexNew] = this.Degs[indexThis].Deep_copy();
            }
            for (int indexAdd=0; indexAdd<add_length; indexAdd++)
            {
                indexNew++;
                new_degs[indexNew] = add_degs[indexAdd].Deep_copy();
            }
            this.Degs = new_degs;
        }

        private Deg_line_class[] Add_fileName(Deg_line_class[] degs, string file_name)
        {
            int degs_length = degs.Length;
            for (int indexDeg = 0; indexDeg < degs_length; indexDeg++)
            {
                degs[indexDeg].Sequencing_run = (string)file_name.Clone();
            }
            return degs;
        }

        public void Generate_by_reading_files()
        {
            string directory = Global_directory_class.Degs_directory;
            string[] complete_file_names = System.IO.Directory.GetFiles(directory);
            string complete_file_name;
            string fileName_without_extension;
            int complete_file_names_length = complete_file_names.Length;
            Deg_line_class[] add_degs;
            for (int indexC=0; indexC<complete_file_names_length; indexC++)
            {
                complete_file_name = complete_file_names[indexC];
                fileName_without_extension = System.IO.Path.GetFileNameWithoutExtension(complete_file_name);
                Deg_readOptions_class readOptions = new Deg_readOptions_class(complete_file_name);
                add_degs = ReadWriteClass.ReadRawData_and_FillArray<Deg_line_class>(readOptions);
                add_degs = Add_fileName(add_degs, fileName_without_extension);
                Add_to_array(add_degs);
            }
        }
        #endregion

        public void Keep_only_input_genes(params string[] keep_genes)
        {
            keep_genes = keep_genes.Distinct().OrderBy(l => l).ToArray();
            this.Degs = this.Degs.OrderBy(l=>l.Gene).ToArray();
            string keep_gene;
            int keep_genes_length = keep_genes.Length;
            int indexKeep = 0;
            int degs_length = this.Degs.Length;
            Deg_line_class deg_line;
            List<Deg_line_class> keep_Degs = new List<Deg_line_class>();
            int stringCompare;
            for (int indexDeg = 0; indexDeg < degs_length; indexDeg++)
            {
                deg_line = this.Degs[indexDeg];
                stringCompare = -2;
                while ((indexKeep < keep_genes_length) && (stringCompare < 0))
                {
                    keep_gene = keep_genes[indexKeep];
                    stringCompare = keep_gene.CompareTo(deg_line.Gene);
                    if (stringCompare < 0)
                    {
                        indexKeep++;
                    }
                    else if (stringCompare == 0)
                    {
                        keep_Degs.Add(deg_line);
                    }
                }
            }
            this.Degs = keep_Degs.ToArray();
        }

        public void Keep_only_input_condition2(params string[] keep_conditions2)
        {
            keep_conditions2 = keep_conditions2.Distinct().OrderBy(l => l).ToArray();
            this.Degs = this.Degs.OrderBy(l => l.Condition2).ToArray();
            string keep_condition2;
            int keep_conditions2_length = keep_conditions2.Length;
            int indexKeep = 0;
            int degs_length = this.Degs.Length;
            Deg_line_class deg_line;
            List<Deg_line_class> keep_Degs = new List<Deg_line_class>();
            int stringCompare;
            for (int indexDeg = 0; indexDeg < degs_length; indexDeg++)
            {
                deg_line = this.Degs[indexDeg];
                stringCompare = -2;
                while ((indexKeep < keep_conditions2_length) && (stringCompare < 0))
                {
                    keep_condition2 = keep_conditions2[indexKeep];
                    stringCompare = keep_condition2.CompareTo(deg_line.Condition2);
                    if (stringCompare < 0)
                    {
                        indexKeep++;
                    }
                    else if (stringCompare == 0)
                    {
                        keep_Degs.Add(deg_line);
                    }
                }
            }
            this.Degs = keep_Degs.ToArray();
        }

        public void Keep_only_input_cellLines(params string[] keep_cellLines)
        {
            keep_cellLines = keep_cellLines.Distinct().OrderBy(l => l).ToArray();
            this.Degs = this.Degs.OrderBy(l => l.Cell).ToArray();
            string keep_cellLine;
            int keep_cellLines_length = keep_cellLines.Length;
            int indexKeep = 0;
            int degs_length = this.Degs.Length;
            Deg_line_class deg_line;
            List<Deg_line_class> keep_Degs = new List<Deg_line_class>();
            int stringCompare;
            for (int indexDeg = 0; indexDeg < degs_length; indexDeg++)
            {
                deg_line = this.Degs[indexDeg];
                stringCompare = -2;
                while ((indexKeep < keep_cellLines_length) && (stringCompare < 0))
                {
                    keep_cellLine = keep_cellLines[indexKeep];
                    stringCompare = keep_cellLine.CompareTo(deg_line.Cell);
                    if (stringCompare < 0)
                    {
                        indexKeep++;
                    }
                    else if (stringCompare == 0)
                    {
                        keep_Degs.Add(deg_line);
                    }
                }
            }
            this.Degs = keep_Degs.ToArray();
        }

        
        public Deg_class Deep_copy()
        {
            Deg_class copy = (Deg_class)this.MemberwiseClone();
            int degs_length = this.Degs.Length;
            copy.Degs = new Deg_line_class[degs_length];
            for (int indexDeg = 0; indexDeg < degs_length; indexDeg++)
            {
                copy.Degs[indexDeg] = this.Degs[indexDeg].Deep_copy();
            }
            return copy;
        }

    }
}
