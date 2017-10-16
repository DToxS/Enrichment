# Enrichment
This is the source code written in C# for enrichment analysis of the differentially expressed genes and proteins that were generated based on the high-throughput mRNA-sequencing datasets and can be downloaded from the DToxS website (www.dtoxs.org).

# Compiling Code
The source code can be compiled using a development environment such as "Microsoft Visual C# 2010".

# Directories
The program will use the directories that are specified in the class "Global_directory_class" that is part of in the reference "Global_parameters". The program will search for files with differentially expressed genes (DEGs) in the folder specified under "Degs_directory". The DEG-files can be downloaded from the DToxS website (www.dtoxs.org). Ontology libraries need to be stored in the directory specified under "Ontology_directory". The Ontology libraries can be obtained from the EnrichR website (http://amp.pharm.mssm.edu/Enrichr/). The file extension ".txt" needs to be added to the file names of the downloaded libraries. Enrichment results will be written in the directory specified under "Results_directory".

# Ontologies
The ontologies that will be used for enrichment analysis can be specified in the "Ontology_process_analysis_options_class" that is part of the reference "Enrichment". Alternatively, they can also be specified in the main program in the reference "Start_enrichment". In the current version the ontologies "Wikipathways_2016", "Reactome_2016", "Kegg_2016", and "GO_biological_process_2017" are considered. Any additional ontology that is downloaded from the EnrichR website can be added by creating a new enum that must match the fileName of the downloaded ontology library.
