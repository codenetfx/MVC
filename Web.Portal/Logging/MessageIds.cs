using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace UL.Aria.Web.Portal.Logging
{
    internal class MessageIds
    {
        public const int ProjectControllerLinkDocumentToTaskException = 44300;
        public const int ProjectControllerRemoveLinkDocumentToTaskException = 44301;
        public const int ProjectControllerAddDocumentToTaskException = 44302;
        public const int ProjectControllerRemoveProjectException = 44303;
        public const int ProjectControllerTaskCreateException = 44304;
        public const int ProjectControllerTaskDeleteException = 44305;
        public const int ProjectControllerServiceLinesRetrieveByIdException = 44306;
        public const int ProjectControllerServiceLinesRetrieveByNumberException = 44307;
        public const int ProjectControllerAddOrderNumberException = 44308;
        public const int ProjectControllerValidateOrderNumberServiceLinesRetrieveByIdException = 44309;
        public const int ProjectControllerValidateOrderNumberServiceLinesRetrieveByNumberException = 44310;
        public const int ProjectControllerCreateException = 44311;
        public const int ProjectControllerEditException = 44312;
        public const int ProjectControllerSearchProductException = 44313;
        public const int ProjectControllerAddProductException = 44314;

        public const int ProfileControllerScratchFileUploadError = 44100;

        public const int ContainerControllerUploadException = 44201;
        public const int ContainerControllerEditMetaDataException = 44202;
        public const int ContainerControllerContainerNotFound = 44203;
        public const int ContainerControllerDeleteDocumentFetchException = 44204;
        public const int ContainerControllerDeleteDocumentException = 44205;
        
        public const int BaseControllerPageGrowlMessage = 44000;
        public const int BaseControllerServiceError = 44001;
        public const int HomeControllerRefinerNotFound = 44011;
           

        public const int CompanyControllerDeleteUserException = 44400;

        public const int ProductControllerUploadFamilyException = 44500;
        public const int ProductControllerUploadInvalid = 44501;
        public const int ProductControllerUploadException = 44502;
        public const int ProductControllerSubmitException = 44503; 
        public const int ProductControllerDeleteException = 44504;

        public const int LegalControllerDeclinedTandC = 44600;
        public const int UserControllerEditContainerAccessPermissionsException = 44650;
        public const int ProfileControllerDeleteFavoriteException = 44651;

        public const int ProjectTemplateCreateException = 44652;


	    public const int CustomerSyncWebRequestStart = 44653;
		public const int CustomerSyncWebRequestException = 44654;
		public const int CustomerSyncProxyStart = 44655;
		public const int CustomerSyncProxyException = 44656;
		public const int CustomerSyncProxyEnd = 44657;

		public const int TaskCategoryException = 44658;

        public const int TaskTypeCreateException = 44659;
        public const int BusinessUnitException = 44670;
        public const int UserTeamException = 44680;
        public const int LinkException = 44675;
		public const int DocumentTemplateException = 44676;

	    public const int TaskCreateDocumentException = 44700;
    }
}
