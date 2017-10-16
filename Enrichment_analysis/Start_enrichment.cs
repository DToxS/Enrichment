/*
Created by Jens Hansen on 10/1/17.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Differentially_expressed_genes;
using Enrichment;
using ReadWrite;

namespace ClassLibrary1
{
    class Analyse_lincs
    {
        public static void Main()
        {
            Deg_class deg = new Deg_class();
            deg.Generate_by_reading_files();

            Ontology_process_analysis_class onto_process = new Ontology_process_analysis_class();
            onto_process.Options.Ontologies = new Ontology_type_enum[] { Ontology_type_enum.GO_biological_process_2017, Ontology_type_enum.Kegg_2016, Ontology_type_enum.Reactome_2016, Ontology_type_enum.Wikipathways_2016 };
            onto_process.Generate();

            Ontology_enrichment_class onto_enrich = onto_process.Do_fisher_exact_test_for_de_instance_filter_and_write_for_individual_ontology(deg);

            onto_enrich.Keep_top_predictions_per_dataset_and_ontology_based_on_pvalue(20);
            onto_enrich.Write_in_results_directory("LINCS_DtoxS_enrichment_results.txt");
        }
    }
}
