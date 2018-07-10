DDI Lifecycle in Colectica Curation Tools
==========================================

The curation system creates DDI 3.2 according the following mappings.

.. contents::
   :depth: 2
   :local:

.. seealso::

   See http://www.ddialliance.org/ for complete documentation on the DDI standard.

---------
StudyUnit
---------

Catalog Record properties map to DDI StudyUnit elements according to the table below.

==================================  =================
Catalog Record Property             StudyUnit Element
==================================  =================
Id                                  Identifier
Title                               Citation/Title
StudyId                             UserID typeOfUserID = ``StudyId``
Number                              UserID typeOfUserID = ``StudyNumber``
Authors                             Citation/Creator
Description                         Citation/Description
Organization                        UserAttributePair (AttributeKey = ``OrganizationName``)
Organization.AgencyID               AgencyId
Keywords                            Coverage/TopicalCoverage/Keyword
CreatedBy                           Citation/Contributor
CreatedDate                         UserAttributePair (AttributeKey = ``CreatedDate``)
Version                             Version
LastUpdatedDate                     VersionDate
Owner                               Citation/Publisher
PersistentId                        UserID typeOfUserID = ``Handle``
Funding                             FundingInformation/Description
ArchiveDate                         UserAttributePair (AttributeKey = ``ArchiveDate``)
PublishDate                         Citation/Date
DepositAgreement                    UserAttributePair (AttributeKey = ``DepositAgreement``)
AccessStatement                     UserAttributePair (AttributeKey = ``AccessStatement``)
ConfidentialityStatement            UserAttributePair (AttributeKey = ``ConfidentialityStatement``)
EmbargoStatement                    Embargo/Description
RelatedDatabase                     UserAttributePair (AttributeKey = ``RelatedDatabase``)
RelatedPublications                 UserAttributePair (AttributeKey = ``RelatedPublications``)
RelatedProjects                     UserAttributePair (AttributeKey = ``RelatedProjects``)
ResearchDesign                      DataCollection/Methodology/Methodology/Description
ModeOfDataCollection                DataCollection/CollectionEvent/ModeOfCollection/Description
FieldDates                          DataCollection/CollectionEvent/DataCollectionDate
StudyTimePeriod                     Coverage/TemporalCoverage/Dates
Location                            Coverage/SpatialCoverage/HighestLevel
LocationDetails                     Coverage/SpatialCoverage/Description
UnitOfObservation                   AnalysisUnit
SampleSize                          UserAttributePair (AttributeKey = ``SampleSize``)
InclusionExclusionCriteria          DataCollection/Methodology/SamplingProcedure/Description
RandomizationProcedure              UserAttributePair (AttributeKey = ``RandomizationProcedure``)
UnitOfRandomization                 UserAttributePair (AttribueKey = ``UnitOfRandomization``)
Treatment                           UserAttributePair (AttribueKey = ``Treatment``)
TreatmentAdministration             UserAttributePair (AttribueKey = ``TreatmentAdministration``)
OutcomeMeasures                     UserAttributePair (AttribueKey = ``OutcomeMeasures``)
Files                               PhysicalInstance (for data files) or OtherMaterial (non-data files)
ReviewType                          UserAttributePair (AttributeKey = ``ReviewType``)
CertifiedDate                       UserAttributePair (AttributeKey = ``CertifiedDate``)
==================================  =================


----------------
PhysicalInstance
----------------

Data file properties map to DDI PhysicalInstance elements according to the table below.

==================================  ========================
File Property                       PhysicalInstance Element
==================================  ========================
Id                                  Identifier
Number                              UserID TypeOfUserID = ``FileNumber``
Title                               Citation/Title
Name                                N/A
PublicName                          Citation/AlternateTitle
PersistentLink                      FileIdentification/Uri
PersistentLinkDate                  UserAttributePair (AttributeKey = ``PersistentLinkDate``)
Version                             Version
Type                                UserAttributePair (AttributeKey = ``FileType``)
FormatName                          UserAttributePair (AttributeKey = ``FormatName``)
FormatId                            UserAttributePair (AttributeKey = ``FormatId``)
Size                                UserAttributePair (AttributeKey = ``Size``)
CreationDate                        UserAttributePair (AttributeKey = ``CreationDate``)
KindOfData                          UserAttributePair (AttributeKey = ``KindOfData``)
Source                              Citation/Source
SourceInformation                   UserAttributePair (AttributeKey = ``SourceInformation``)
Rights                              Citation/Rights
IsPublicAccess                      FileIdentification/IsPublic
UploadedDate                        UserAttributePair (AttributeKey = ``UploadedDate``) 
ExternalDatabase                    UserAttributePair (AttributeKey = ``ExternalDatabase``) 
Software                            UserAttributePair (AttributeKey = ``Software``) 
SoftwareVersion                     UserAttributePair (AttributeKey = ``SoftwareVersion``) 
Hardware                            UserAttributePair (AttributeKey = ``Hardware``) 
Checksum                            Fingerprint/FingerprintValue
ChecksumMethod                      Fingerprint/AlgorithmSpecification
ChecksumDate                        UserAttributePair (AttributeKey = ``ChecksumDate``) 
VirusCheckOutcome                   UserAttributePair (AttributeKey = ``VirusCheckOutcome``) 
VirusCheckMethod                    UserAttributePair (AttributeKey = ``VirusCheckMethod``) 
VirusCheckDate                      UserAttributePair (AttributeKey = ``VirusCheckDate``) 
AcceptedDate                        UserAttributePair (AttributeKey = ``AcceptedDate``) 
CertifiedDate                       UserAttributePair (AttributeKey = ``CertifiedDate``) 
==================================  ========================

----------------
OtherMaterial
----------------

Non-data file properties map to DDI OtherMaterial elements according to the table below.

.. note::

    In DDI, the :dfn:`OtherMaterial` type does not support
    UserAttributes. Therefore, to store information not built in to
    the OtherMaterial type, this mapping uses the :dfn:`UserID` as a
    key-value store. This is not semantically ideal, but is an interim
    solution until a future version of DDI supports saving extended
    information about OtherMaterial items.

==================================  =====================
File Property                       OtherMaterial Element
==================================  =====================
Id                                  Identifier
Number                              UserID TypeOfUserID = ``FileNumber``
Title                               Citation/Title
Name                                N/A
PublicName                          Citation/AlternateTitle
PersistentLink                      URL
PersistentLinkDate                  UserID TypeOfUserID = ``PersistentLinkDate``
Version                             UserID TypeOfUserID = ``Version``
Type                                UserID TypeOfUserID = ``Type``
FormatName                          UserID TypeOfUserID = ``FormatName``
FormatId                            UserID TypeOfUserID = ``FormatId``
Size                                UserID TypeOfUserID = ``Size``
CreationDate                        UserID TypeOfUserID = ``CreationDate``
KindOfData                          UserID TypeOfUserID = ``KindOfData``
Source                              Citation/Source
SourceInformation                   UserID TypeOfUserID = ``SourceInformation``
Rights                              Citation/Rights
IsPublicAccess                      UserID TypeOfUserID = ``IsPublicAccess``     
UploadedDate                        UserID TypeOfUserID = ``UploadedDate``   
ExternalDatabase                    UserID TypeOfUserID = ``ExternalDatabase``       
Software                            UserID TypeOfUserID = ``Software``       
SoftwareVersion                     UserID TypeOfUserID = ``SoftwareVersion``      
Hardware                            UserID TypeOfUserID = ``Hardware``       
Checksum                            UserID TypeOfUserID = ``Checksum``       
ChecksumMethod                      UserID TypeOfUserID = ``ChecksumMethod``     
ChecksumDate                        UserID TypeOfUserID = ``ChecksumDate``   
VirusCheckOutcome                   UserID TypeOfUserID = ``VirusCheckOutcome``        
VirusCheckMethod                    UserID TypeOfUserID = ``VirusCheckMethod``       
VirusCheckDate                      UserID TypeOfUserID = ``VirusCheckDate``     
AcceptedDate                        UserID TypeOfUserID = ``AcceptedDate``   
CertifiedDate                       UserID TypeOfUserID = ``CertifiedDate``    
==================================  =====================


----------------
Variable
----------------

Variable properties map to DDI Variable elements according to the table below.

==================================  =================
Variable Property                   Variable Element
==================================  =================
Id                                  Id
Agency                              Agency
Version                             Version
LastUpdated                         VersionDate
Name                                ItemName
Label                               Label
Description                         Description
ResponseUnit                        ResponseUnit
AnalysisUnit                        AnalysisUnit
ClassificationLevel                 Representation/ClassificationLevel
RepresentationType                  Representation
Valid                               VariableStatistic/Valid
Invalid                             VariableStatistic/Invalid
Minimum                             VariableStatistic/Minimum
Maximum                             VariableStatistic/Maximum
Mean                                VariableStatistic/Mean
StandardDeviation                   VariableStatistic/StandardDeviation
Category                            VariableStatistic/UnfilteredCategoryStatistic/Category
Frequency                           VariableStatistic/UnfilteredCategoryStatistic/Frequency
==================================  =================
