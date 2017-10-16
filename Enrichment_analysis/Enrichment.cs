/*
Created by Jens Hansen on 10/1/17.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Global_parameter;
using Report;
using ReadWrite;
using Differentially_expressed_genes;

namespace Enrichment
{

    enum Ontology_type_enum { E_m_p_t_y, Wikipathways_2016, Reactome_2016, Kegg_2016, GO_biological_process_2017 }
    enum ProcessPrediction_diffExpr_enum  { E_m_p_t_y, Upregulated, Downregulated, All }

    class Fisher_exact_test_class
    {
        private double[] log_factorials;
        private int max_size;
        public bool Report;

        //         a     b
        //         c     d

        public Fisher_exact_test_class(int input_max_size, bool report)
        {
            max_size = input_max_size;
            Report = report;
            if (Report)
            {
                Report_class.WriteLine("{0}: Initialize array of factorials with max_size = {1}", typeof(Fisher_exact_test_class).Name, max_size);
            }
            log_factorials = new double[max_size + 1];
            log_factorials[0] = 0;
            for (int i = 1; i < max_size + 1; i++)
            {
                log_factorials[i] = log_factorials[i - 1] + Math.Log(i);
            }
        }

        private bool Check_if_n_not_larger_than_max_size(int a, int b, int c, int d)
        {
            bool smaller = true;
            int n = a + b + c + d;
            if (n > max_size + 1)
            {
                Report_class.Write_error_line("{0}: n ({1}) is larger than max_size ({2}): initialize new fisher exact test instance", typeof(Fisher_exact_test_class).Name, n, max_size);
                smaller = false;
            }
            return smaller;
        }

        private double Get_specific_log_p_value(int a, int b, int c, int d)
        {
            int n = a + b + c + d;
            double log_p = log_factorials[a + b] + log_factorials[c + d] + log_factorials[a + c] + log_factorials[b + d] - log_factorials[n] - log_factorials[a] - log_factorials[b] - log_factorials[c] - log_factorials[d];
            return log_p;
        }

        private double Get_specific_p_value(int a, int b, int c, int d)
        {
            double log_p = Get_specific_log_p_value(a, b, c, d);
            return Math.Exp(log_p);
        }

        public double Get_rightTailed_p_value(int a, int b, int c, int d)
        {
            if (Report) { Report_class.WriteLine("{0}: Get right tailed p-value", typeof(Fisher_exact_test_class).Name); }
            double p;
            if (Check_if_n_not_larger_than_max_size(a, b, c, d))
            {
                p = Get_specific_p_value(a, b, c, d);
                int min = (c < b) ? c : b;
                for (int i = 0; i < min; i++)
                {
                    p += Get_specific_p_value(++a, --b, --c, ++d);
                }
            }
            else { p = -1; };
            if (Report)
            {
                for (int i = 0; i < typeof(Fisher_exact_test_class).Name.Length + 2; i++) { Report_class.Write(" "); }
                Report_class.WriteLine("p_value: {0}", p);
                for (int i = 0; i < typeof(Fisher_exact_test_class).Name.Length + 2; i++) { Report_class.Write(" "); }
                Report_class.WriteLine("log10_p_value: {0}", Math.Log10(p));
            }
            if (p > 1) { p = 1; }
            return p;
        }

        public double Get_leftTailed_p_value(int a, int b, int c, int d)
        {
            if (Report) { Report_class.WriteLine("{0}: Get left tailed p-value", typeof(Fisher_exact_test_class).Name); }
            double p;
            if (Check_if_n_not_larger_than_max_size(a, b, c, d))
            {
                p = Get_specific_p_value(a, b, c, d);
                int min = (a < d) ? a : d;
                for (int i = 0; i < min; i++)
                {
                    p += Get_specific_p_value(--a, ++b, ++c, --d);
                }
            }
            else { p = -1; };
            if (Report)
            {
                for (int i = 0; i < typeof(Fisher_exact_test_class).Name.Length + 2; i++) { Report_class.Write(" "); }
                Report_class.WriteLine("p_value: {0}", p);
            }
            if (p > 1) { p = 1; }
            return p;
        }
    }

    interface IGO_fisher_exact_line
    {
        string Name { get; set; }
        string GO_id { get; set; }
        string GO_name { get; set; }
        int ClusterSize { get; set; }
        int ClusterSize_of_GO_proteins { get; set; }
        int Final_GO_size { get; set; }
        int Overlap_size { get; set; }
        string[] Overlap_symbols { get; set; }
        double Minus_log10_p_value { get; set; }
        double Odds_ratio { get; set; }
        double Z_score { get; set; }
        double P_value { get; set; }
        double Q_value { get; set; }
        double FDR { get; set; }
        double Bonferroni { get; set; }
        Ontology_type_enum Ontology { get; set; }
    }

    class Ontology_fisher_exact_options_class
    {
        #region Fields
        public bool Report { get; set; }
        public int MinProteins_per_GO { get; set; }
        public int MaxProteins_per_GO { get; set; }
        public string[] AcceptedProteins { get; set; }
        #endregion

        public Ontology_fisher_exact_options_class()
        {
            Report = true;
        }
    }

    class Ontology_fisher_exact_class
    {
        #region Fields
        public Ontology_association_class Ontology_association { get; private set; }
        public Ontology_fisher_exact_options_class Options { get; private set; }
        Fisher_exact_test_class Fisher { get; set; }
        public string[] Background_genes { get; set; }
        #endregion

        #region Constructor
        public Ontology_fisher_exact_class(Ontology_type_enum onto_type, params string[] data_bg_genes)
        {
            this.Options = new Ontology_fisher_exact_options_class();
            this.Ontology_association = new Ontology_association_class(onto_type);
            this.Ontology_association.Generate_by_reading_safed_file();
            string[] ontology_bg_genes = this.Ontology_association.Get_all_ordered_distinct_genes();
            if (data_bg_genes.Length==0)
            {
                int ontology_bg_genes_length = ontology_bg_genes.Length;
                this.Background_genes = new string[ontology_bg_genes_length];
                for (int indexBg = 0; indexBg < ontology_bg_genes_length; indexBg++)
                {
                    this.Background_genes[indexBg] = (string)ontology_bg_genes[indexBg].Clone();
                }
            }
            else
            {
                this.Background_genes = Overlap_class.Get_intersection(data_bg_genes, ontology_bg_genes);
            }
        }
        #endregion

        #region Generate and filter ga nw for acceptedGenes
        public void Generate_new_instance()
        {
            if (Options.Report)
            {
                Report_class.WriteLine("-------------------------------------------------------------------------------");
                Report_class.WriteLine("{0}: Generate new instance based on {1}", typeof(Ontology_fisher_exact_class).Name, this.Ontology_association.Ontology);
            }
            this.Ontology_association.Keep_only_input_genes_in_process_gene_association(this.Background_genes);
            this.Fisher = new Fisher_exact_test_class(this.Background_genes.Length, false);
        }
        #endregion

        #region Calculate p values
        public Ontology_enrichment_line_class[] Calculate_p_values_and_do_mutliple_hypothesis_correcion_for_input_genes(string[] inputGenes, Deg_line_class first_deg_line)
        {
            inputGenes = inputGenes.OrderBy(l=>l).ToArray();
            string inputGene;
            int inputGenes_length = inputGenes.Length;
            int indexInput=0;
            int stringCompare = -2;

            int overlap_count = 0;
            int inputGenes_count = inputGenes.Length;
            int processGenes_count = 0;
            int a; int b; int c; int d;
            int background_genes_length = this.Background_genes.Length;
            int processes_count=0;

            int process_gene_association_length = this.Ontology_association.Process_gene_associations.Length;
            Ontology_association_line_class onto_asso_line;

            Ontology_enrichment_line_class onto_enrichment_line;
            List<Ontology_enrichment_line_class> enrichment_list = new List<Ontology_enrichment_line_class>();
            List<string> overlap_genes = new List<string>();


            #region Caculate p-values
            for (int indexOnto=0; indexOnto<process_gene_association_length; indexOnto++)
            {
                onto_asso_line = this.Ontology_association.Process_gene_associations[indexOnto];
                if ((indexOnto==0) || (!onto_asso_line.ProcessName.Equals(this.Ontology_association.Process_gene_associations[indexOnto-1].ProcessName)))
                {
                    processes_count++;
                    processGenes_count = 0;
                    overlap_count = 0;
                    overlap_genes.Clear();
                    indexInput = 0;
                }
                if (  (indexOnto!=0)
                    &&(onto_asso_line.ProcessName.Equals(this.Ontology_association.Process_gene_associations[indexOnto-1].ProcessName))
                    &&(onto_asso_line.GeneSymbol.CompareTo(this.Ontology_association.Process_gene_associations[indexOnto-1].GeneSymbol) <=0 ))
                {
                    throw new Exception(); // duplicated gene association with process or gene symbols are not sorted properly
                }
                stringCompare = -2;
                processGenes_count++;
                while ((indexInput<inputGenes_length) && (stringCompare<0))
                {
                    inputGene = inputGenes[indexInput];
                    stringCompare = inputGene.ToUpper().CompareTo(onto_asso_line.GeneSymbol.ToUpper());
                    if (stringCompare<0)
                    {
                        indexInput++;
                    }
                    else if (stringCompare==0)
                    {
                        overlap_count++;
                        overlap_genes.Add(inputGene);
                        if (overlap_genes.Distinct().ToArray().Length != overlap_genes.Count)
                        {
                            throw new Exception();
                        }
                    }
                }
                if ((indexOnto==process_gene_association_length-1) || (!onto_asso_line.ProcessName.Equals(this.Ontology_association.Process_gene_associations[indexOnto+1].ProcessName)))
                {
                    if (overlap_count > 0)
                    {
                        a = overlap_count;
                        b = processGenes_count - overlap_count;
                        c = inputGenes_length - overlap_count;
                        d = background_genes_length - a - b - c;

                        if ((a < 0) || (b < 0) || (c < 0) || (d < 0))
                        {
                            throw new Exception();
                        }
                        onto_enrichment_line = new Ontology_enrichment_line_class();
                        onto_enrichment_line.Ontology = this.Ontology_association.Ontology;
                        onto_enrichment_line.Overlap_count = overlap_count;
                        onto_enrichment_line.ProcessName = (string)onto_asso_line.ProcessName.Clone();
                        onto_enrichment_line.P_value = Fisher.Get_rightTailed_p_value(a, b, c, d);
                        onto_enrichment_line.Minus_log10_pvalue = -Math.Log10(onto_enrichment_line.P_value);
                        onto_enrichment_line.Sequencing_run = (string)first_deg_line.Sequencing_run.Clone();
                        onto_enrichment_line.Cell = (string)first_deg_line.Cell.Clone();
                        onto_enrichment_line.Condition1 = (string)first_deg_line.Condition1.Clone();
                        onto_enrichment_line.Condition2 = (string)first_deg_line.Condition2.Clone();
                        onto_enrichment_line.Overlap_genes = overlap_genes.OrderBy(l => l).ToArray();

                        enrichment_list.Add(onto_enrichment_line);
                    }
                }
            }
            #endregion

            #region Calculate q-values and bonferroni
            enrichment_list = enrichment_list.OrderBy(l=>l.P_value).ToList();
            int enrichment_length = enrichment_list.Count;
            List<int> rank_list = new List<int>();
            int rank = 0;
            int first_index_of_identical_p_value = 0;
            Ontology_enrichment_line_class inner_onto_line;
            for (int indexO = 0; indexO < enrichment_length; indexO++)
            {
                onto_enrichment_line = enrichment_list[indexO];

                //Bonferroni
                onto_enrichment_line.Bonferroni = onto_enrichment_line.P_value * enrichment_length;
                if (onto_enrichment_line.Bonferroni > 1) { onto_enrichment_line.Bonferroni = 1; }

                //Qvalue
                if (  (indexO == 0) 
                    ||(!onto_enrichment_line.P_value.Equals(enrichment_list[indexO - 1].P_value)))
                {
                    rank_list.Clear();
                    first_index_of_identical_p_value = indexO;
                }

                rank++;
                rank_list.Add(rank);

                if (  (indexO == enrichment_length - 1)
                    ||(onto_enrichment_line.P_value != enrichment_list[indexO + 1].P_value))
                {
                    float rank_average = (float)rank_list.Average();
                    for (int indexInnerO = first_index_of_identical_p_value; indexInnerO <= indexO; indexInnerO++)
                    {
                        inner_onto_line = enrichment_list[indexInnerO];
                        inner_onto_line.Q_value = inner_onto_line.P_value * (processes_count / (float)rank_list.Average());
                        if (inner_onto_line.Q_value > 1) { inner_onto_line.Q_value = 1; }
                    }
                }
            }
            #endregion

            #region Calculabe FDR
            enrichment_list = enrichment_list.OrderBy(l=>l.P_value).ToList();
            double smallest_q_value = -1;
            for (int indexO = enrichment_length - 1; indexO >= 0; indexO--)
            {
                onto_enrichment_line = enrichment_list[indexO];
                if ((indexO == enrichment_length - 1) || (onto_enrichment_line.Q_value < smallest_q_value))
                {
                    smallest_q_value = onto_enrichment_line.Q_value;
                }
                if (smallest_q_value==-1) { throw new Exception(); }
                onto_enrichment_line.False_discovery_rate = smallest_q_value;
            }
            #endregion

            return enrichment_list.ToArray();
        }

        public Ontology_enrichment_class Calculate_p_values_and_do_multiple_hypothesis_correction(Deg_class deg_input)
        {
            if (Options.Report)
            {
                Report_class.Write_major_separation_line();
                Report_class.WriteLine("{0}: Calculate p values and perform multiple hypothesis correction for {1}", typeof(Ontology_fisher_exact_class).Name, this.Ontology_association.Ontology);
            }
            Deg_class deg = deg_input.Deep_copy();
            deg.Keep_only_input_genes(this.Background_genes);
            this.Ontology_association.Process_gene_associations = this.Ontology_association.Process_gene_associations.OrderBy(l=>l.ProcessName).ThenBy(l=>l.GeneSymbol).ToArray();
            deg.Degs = deg.Degs.OrderBy(l=>l.Sequencing_run).ThenBy(l=>l.Cell).ThenBy(l=>l.Condition1).ThenBy(l=>l.Condition2).ToArray();
            int degs_length = deg.Degs.Length;
            Deg_line_class deg_line;
            List<string> inputGenes = new List<string>();
            Ontology_enrichment_line_class[] add_enrichment_results;
            List<Ontology_enrichment_line_class> enrichment_results = new List<Ontology_enrichment_line_class>();
            for (int indexDeg=0; indexDeg<degs_length; indexDeg++)
            {
                deg_line = deg.Degs[indexDeg];
                if (  (indexDeg==0) 
                    ||(!deg_line.Sequencing_run.Equals(deg.Degs[indexDeg-1].Sequencing_run))
                    ||(!deg_line.Cell.Equals(deg.Degs[indexDeg-1].Cell))
                    ||(!deg_line.Condition1.Equals(deg.Degs[indexDeg-1].Condition1))
                    ||(!deg_line.Condition2.Equals(deg.Degs[indexDeg-1].Condition2)))
                {
                    inputGenes.Clear();
                }
                if (  (indexDeg!=0)
                    &&(!deg_line.Sequencing_run.Equals(deg.Degs[indexDeg-1].Sequencing_run))
                    &&(!deg_line.Cell.Equals(deg.Degs[indexDeg-1].Cell))
                    &&(!deg_line.Condition1.Equals(deg.Degs[indexDeg-1].Condition1))
                    &&(!deg_line.Condition2.Equals(deg.Degs[indexDeg-1].Condition2))
                    &&(deg_line.Gene.Equals(deg_line.Gene)))
                {
                    throw new Exception(); //duplicated gene in same condition
                }
                inputGenes.Add(deg_line.Gene);
                if (  (indexDeg==degs_length-1) 
                    ||(!deg_line.Sequencing_run.Equals(deg.Degs[indexDeg+1].Sequencing_run))
                    ||(!deg_line.Cell.Equals(deg.Degs[indexDeg+1].Cell))
                    ||(!deg_line.Condition1.Equals(deg.Degs[indexDeg+1].Condition1))
                    ||(!deg_line.Condition2.Equals(deg.Degs[indexDeg+1].Condition2)))
                {
                    add_enrichment_results = Calculate_p_values_and_do_mutliple_hypothesis_correcion_for_input_genes(inputGenes.ToArray(),deg_line);
                    enrichment_results.AddRange(add_enrichment_results);
                }
            }
            Ontology_enrichment_class onto_enrich = new Ontology_enrichment_class();
            onto_enrich.Add_to_array(enrichment_results.ToArray());
            return onto_enrich;
        }
        #endregion
    }

    /// <summary>
    /// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>

    class Ontology_enrichment_line_class
    {
        public Ontology_type_enum Ontology { get; set; }
        public string ProcessName { get; set; }
        public string Condition1 { get; set; }
        public string Condition2 { get; set; }
        public string Cell { get; set; }
        public string Sequencing_run { get; set; }
        public int Overlap_count { get; set; }
        public double P_value { get; set; }
        public double Q_value { get; set; }
        public double Minus_log10_pvalue { get; set; }
        public double False_discovery_rate { get; set; }
        public double Bonferroni { get; set; }
        public string[] Overlap_genes { get; set; }

        public string ReadWrite_overlap_genes
        {
            get { return ReadWriteClass.Get_writeLine_from_array(this.Overlap_genes, Ontology_enrichment_readWriteOptions_class.Array_delimiter); }
        }

        public Ontology_enrichment_line_class Deep_copy()
        {
            Ontology_enrichment_line_class copy = (Ontology_enrichment_line_class)this.MemberwiseClone();
            copy.ProcessName = (string)this.ProcessName.Clone();
            return copy;
        }
    }

    class Ontology_enrichment_readWriteOptions_class : ReadWriteOptions_base
    {
        public static char Array_delimiter { get { return ',';}}

        public Ontology_enrichment_readWriteOptions_class(string fileName)
        {
            this.File = Global_directory_class.Results_directory + fileName;
            this.Key_propertyNames = new string[] { "Sequencing_run", "Cell", "Condition1", "Condition2", "Ontology", "ProcessName", "Overlap_count", "P_value", "Minus_log10_pvalue", "False_discovery_rate", "Bonferroni", "ReadWrite_overlap_genes" };
            this.Key_columnNames = new string[] { "Sequencing run", "Cell", "Condition1", "Condition2", "Ontology", "Process name", "Overlap count", "Pvalue", "Minus log10(pvalue)", "False discovery rate", "Bonferroni", "Overlap genes" };
            this.HeadlineDelimiters = new char[] { Global_class.Tab };
            this.LineDelimiters = new char[] { Global_class.Tab };
            this.Report = ReadWrite_report_enum.Report_main;
            this.File_has_headline = true;
        }
    }

    class Ontology_enrichment_class
    {
        public Ontology_enrichment_line_class[] Onto_enrich { get; set; }

        public Ontology_enrichment_class()
        {
            this.Onto_enrich = new Ontology_enrichment_line_class[0];
        }

        public void Add_to_array(Ontology_enrichment_line_class[] add_onto_enrich)
        {
            int this_length = this.Onto_enrich.Length;
            int add_length = add_onto_enrich.Length;
            int new_length = this_length + add_length;
            int indexNew = -1;
            Ontology_enrichment_line_class[] new_onto_enrich = new Ontology_enrichment_line_class[new_length];
            for (int indexThis = 0; indexThis < this_length; indexThis++)
            {
                indexNew++;
                new_onto_enrich[indexNew] = this.Onto_enrich[indexThis];
            }
            for (int indexAdd = 0; indexAdd < add_length; indexAdd++)
            {
                indexNew++;
                new_onto_enrich[indexNew] = add_onto_enrich[indexAdd];
            }
            this.Onto_enrich = new_onto_enrich;
        }

        public void Add_other(Ontology_enrichment_class other_onto_enrich)
        {
            Add_to_array(other_onto_enrich.Onto_enrich);
        }

        public void Keep_top_predictions_per_dataset_and_ontology_based_on_pvalue(int top_predictions)
        {
            this.Onto_enrich = this.Onto_enrich.OrderBy(l => l.Ontology).ThenBy(l => l.Sequencing_run).ThenBy(l => l.Cell).ThenBy(l => l.Condition1).ThenBy(l => l.Condition2).ThenBy(l => l.P_value).ToArray();
            int onto_enrich_length = this.Onto_enrich.Length;
            Ontology_enrichment_line_class onto_enrich_line;
            List<Ontology_enrichment_line_class> keep = new List<Ontology_enrichment_line_class>();
            int prediction_ordinary_rank = 0;
            for (int indexO = 0; indexO < onto_enrich_length; indexO++)
            {
                onto_enrich_line = this.Onto_enrich[indexO];
                if ((indexO == 0)
                    || (!onto_enrich_line.Ontology.Equals(this.Onto_enrich[indexO - 1].Ontology))
                    || (!onto_enrich_line.Sequencing_run.Equals(this.Onto_enrich[indexO - 1].Sequencing_run))
                    || (!onto_enrich_line.Condition1.Equals(this.Onto_enrich[indexO - 1].Condition1))
                    || (!onto_enrich_line.Condition2.Equals(this.Onto_enrich[indexO - 1].Condition2)))
                {
                    prediction_ordinary_rank = 0;
                }

                prediction_ordinary_rank++;
                if (prediction_ordinary_rank <= top_predictions)
                {
                    keep.Add(onto_enrich_line);
                }
            }
            this.Onto_enrich = keep.ToArray();
        }

        public void Write_in_results_directory(string fileName)
        {
            Ontology_enrichment_readWriteOptions_class readWriteOptions = new Ontology_enrichment_readWriteOptions_class(fileName);
            ReadWriteClass.WriteData(this.Onto_enrich, readWriteOptions);
        }
    }

    /// <summary>
    /// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>
    
    class Ontology_association_line_class
    {
        public string ProcessName { get; set; }
        public string GeneSymbol { get; set; }

        public Ontology_association_line_class Deep_copy()
        {
            Ontology_association_line_class copy = (Ontology_association_line_class)this.MemberwiseClone();
            copy.ProcessName = (string)this.ProcessName.Clone();
            copy.GeneSymbol = (string)this.GeneSymbol.Clone();
            return copy;
        }
    }

    class Ontology_association_class
    {
        public Ontology_association_line_class[] Process_gene_associations { get; set; }
        public Ontology_type_enum  Ontology { get; set; }

        public Ontology_association_class(Ontology_type_enum ontology)
        {
            this.Ontology = ontology;
        }

        public void Keep_only_input_genes_in_process_gene_association(string[] keep_genes)
        {
            keep_genes = keep_genes.OrderBy(l => l).ToArray();
            string keep_gene;
            int indexK=0;
            int keep_genes_length = keep_genes.Length;
            this.Process_gene_associations = this.Process_gene_associations.OrderBy(l => l.GeneSymbol).ToArray();
            int process_gene_associations_length = this.Process_gene_associations.Length;
            Ontology_association_line_class onto_association_line;
            List<Ontology_association_line_class> keep = new List<Ontology_association_line_class>();
            int stringCompare = -2;
            for (int indexP = 0; indexP < process_gene_associations_length; indexP++)
            {
                onto_association_line = this.Process_gene_associations[indexP];
                stringCompare = -2;
                while ((indexK < keep_genes_length) && (stringCompare < 0))
                {
                    keep_gene = keep_genes[indexK];
                    stringCompare = keep_gene.CompareTo(onto_association_line.GeneSymbol);
                    if (stringCompare < 0)
                    {
                        indexK++;
                    }
                    else if (stringCompare == 0)
                    {
                        keep.Add(onto_association_line);
                    }
                }
            }
            this.Process_gene_associations = keep.ToArray();
        }

        public string[] Get_all_ordered_distinct_genes()
        {
            this.Process_gene_associations = this.Process_gene_associations.OrderBy(l=>l.GeneSymbol).ToArray();
            int this_length = this.Process_gene_associations.Length;
            Ontology_association_line_class process_gene_association_line;
            List<string> genes = new List<string>();
            for (int indexThis=0; indexThis < this_length; indexThis++)
            {
                process_gene_association_line = this.Process_gene_associations[indexThis];
                if ((indexThis==0) || (!process_gene_association_line.GeneSymbol.Equals(this.Process_gene_associations[indexThis-1].GeneSymbol)))
                {
                    genes.Add(process_gene_association_line.GeneSymbol);
                }
            }
            return genes.ToArray();
        }

        private void Remove_duplicates()
        {
            this.Process_gene_associations = this.Process_gene_associations.OrderBy(l => l.ProcessName).ThenBy(l => l.GeneSymbol).ToArray();
            int process_gene_associations_length = this.Process_gene_associations.Length;
            Ontology_association_line_class onto_line;
            List<Ontology_association_line_class> keep = new List<Ontology_association_line_class>();
            for (int indexP = 0; indexP < process_gene_associations_length; indexP++)
            {
                onto_line = this.Process_gene_associations[indexP];
                if ((indexP == 0)
                    || (!onto_line.ProcessName.Equals(this.Process_gene_associations[indexP - 1].ProcessName))
                    || (!onto_line.GeneSymbol.Equals(this.Process_gene_associations[indexP - 1].GeneSymbol)))
                {
                    keep.Add(onto_line);
                }
            }
            this.Process_gene_associations = keep.ToArray();
        }

        private void Remove_mus_musculus_processes()
        {
            this.Process_gene_associations = this.Process_gene_associations.OrderBy(l => l.ProcessName).ThenBy(l => l.GeneSymbol).ToArray();
            int process_gene_associations_length = this.Process_gene_associations.Length;
            Ontology_association_line_class onto_line;
            List<Ontology_association_line_class> keep = new List<Ontology_association_line_class>();
            for (int indexP = 0; indexP < process_gene_associations_length; indexP++)
            {
                onto_line = this.Process_gene_associations[indexP];
                if (onto_line.ProcessName.ToUpper().IndexOf("Mus musculus".ToUpper()) == -1)
                {
                    keep.Add(onto_line);
                }
            }
            this.Process_gene_associations = keep.ToArray();
        }

        public void Generate_by_reading_safed_file()
        {
            Read_file();
            Remove_duplicates();
            Remove_mus_musculus_processes();
        }

        private void Read_file()
        {
            string complete_fileName = Global_directory_class.Ontology_directory + this.Ontology.ToString() + ".txt";
            StreamReader reader = new StreamReader(complete_fileName);
            string inputLine;
            string[] columnEntries;
            string columnEntry;
            int columnEntries_length;
            string processName;
            string geneSymbol;

            Ontology_association_line_class onto_association_line;
            List<Ontology_association_line_class> onto_associations = new List<Ontology_association_line_class>();
            while ((inputLine = reader.ReadLine()) != null)
            {
                columnEntries = inputLine.Split(Global_class.Tab);
                columnEntries_length = columnEntries.Length;
                processName = columnEntries[0].Split('(')[0];
                while (processName.Substring(processName.Length - 1, 1) == " ")
                {
                    processName = processName.Substring(0, processName.Length - 1);
                }
                for (int indexC = 1; indexC < columnEntries_length; indexC++)
                {
                    columnEntry = columnEntries[indexC];
                    if (!String.IsNullOrEmpty(columnEntry))
                    {
                        geneSymbol = columnEntry.Split(',')[0];
                        onto_association_line = new Ontology_association_line_class();
                        onto_association_line.GeneSymbol = (string)geneSymbol.Clone();
                        onto_association_line.ProcessName = (string)processName.Clone();
                        onto_associations.Add(onto_association_line);
                    }
                }
            }
            this.Process_gene_associations = onto_associations.ToArray();
        }
    }

    /// <summary>
    /// ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /// </summary>

    class Ontology_process_analysis_options_class
    {
        #region Fields
        public Ontology_type_enum[] Ontologies { get; set; }

        public bool Report { get; set; }
        #endregion

        public Ontology_process_analysis_options_class()
        {
            Ontologies = new Ontology_type_enum[] { Ontology_type_enum.GO_biological_process_2017, Ontology_type_enum.Wikipathways_2016, Ontology_type_enum.Reactome_2016 };//   
            Report = true;
        }

        private void Print_options()
        {
            Report_class.WriteLine("{0}: Print options", typeof(Ontology_process_analysis_options_class).Name);
            for (int i = 0; i < typeof(Ontology_process_analysis_options_class).Name.Length + 2; i++) { Report_class.Write(" "); }
            Report_class.Write("Used Ontologies:");
            for (int i = 0; i < Ontologies.Length; i++) { Report_class.Write(" {0}", Ontologies[i]); }
            Report_class.WriteLine();
        }
    }

    class Ontology_process_analysis_class
    {
        #region Fields
        public Ontology_fisher_exact_class[] Ontology_fishers { get; set; }
        public Ontology_process_analysis_options_class Options { get; set; }
        #endregion

        public Ontology_process_analysis_class()
        {
            Options = new Ontology_process_analysis_options_class();
        }

        #region Generate
        private void Generate_fisher_excat_intances(params string[] experimental_background_genes)
        {
            if (Options.Report)
            {
                Report_class.WriteLine("{0}: Generate fisher exact instances",typeof(Ontology_process_analysis_class).Name);
            }
            experimental_background_genes = experimental_background_genes.Distinct().OrderBy(l => l).ToArray();

            Ontology_type_enum[] ontologies = this.Options.Ontologies;
            int ontologies_length = ontologies.Length;
            Ontology_type_enum ontology;

            this.Ontology_fishers = new Ontology_fisher_exact_class[ontologies_length];

            for (int indexO = 0; indexO < ontologies_length; indexO++)
            {
                ontology = ontologies[indexO];
                this.Ontology_fishers[indexO] = new Ontology_fisher_exact_class(ontology, experimental_background_genes);
                this.Ontology_fishers[indexO].Generate_new_instance();
            }
            if (Options.Report)
            {
                Report_class.WriteLine();
            }
        }

        public void Generate(params string[] background_genes)
        {
            Report_class.WriteLine("{0}: Generate", typeof(Ontology_process_analysis_class).Name);
            Generate_fisher_excat_intances(background_genes);
        }
        #endregion

        #region Do fisher exact test and filter
        public Ontology_enrichment_class Do_fisher_exact_test_for_de_instance_filter_and_write_for_individual_ontology(Deg_class deg)
        {
            int ontologies_length = this.Ontology_fishers.Length;
            Ontology_fisher_exact_class fisher;
            Ontology_enrichment_class onto_enrich = new Ontology_enrichment_class();
            Ontology_enrichment_class onto_enrich_add;
            for (int indexO=0; indexO<ontologies_length; indexO++)
            {
                fisher = this.Ontology_fishers[indexO];
                onto_enrich_add = fisher.Calculate_p_values_and_do_multiple_hypothesis_correction(deg);
                onto_enrich.Add_other(onto_enrich_add);
            }
            return onto_enrich;
        }
        #endregion

    }
}
