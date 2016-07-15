/// <reference path="_references.js" />
/// <reference path="Widgets/UL.MvcJqGrid.js" />
/// <reference path="Widgets/UL.AccordionMenu.js" />
/// <reference path="Widgets/UL.Autocomplete.js" />
/// <reference path="Widgets/UL.DualListBox.js" />

(function (jquery) {
    'use strict';

    if (!window.UL) {
        window.UL = {};
    }

    UL.BootstrapControls = function bootstrapControls($) {
        /// <summary>
        /// initializes all matching conrols to the jQuery searchs scoped to the parameter
        /// Note: Call this function for any dynamic html that needs the conrols applied to it's content.
        /// For example: A modal loaded after the page is initialized.
        /// This method is ran by default at page load.
        /// </summary>
        /// <param name="$" type="jQuery">Scoped to the jQuery's results.</param>

        $.find('.ellipsis-ctrl').ellipsis();

        $.find('.ellipsis-add').ellipsisAdd();

    	//initializes jqgrid's wrapped with the mvc helper.
        $.find(".mvc-jqgrid").mvcJqGrid();

        //initilizes UL accordionMenu compoent e.g.(left nav)
        $.find(".accordion-menu").accordionMenu();


        //initializes the filter Manager/menu - the if statement
        //is in place to support a scenerio where there are no filter support on the page
        var filterManagers = $.find(".filter-manager");
        if (filterManagers.length > 0) {
            var fm = $.find(".filter-manager").filterManager();
            $.find(".auxiliary-filter-btn").auxiliaryFilterBtn(fm);
        }
        else {
            $.find(".search-btn").click(function () {
                $.find("#frmSearchTop").submit();
            });
            $.find('.filter-sort-btn').on("click", function () { UL.Sort(this); });
            $.find('.refine-list .primary-refiner-a').on('click', function(e, ui) {
                UL.RefineSearch(this);
            });
        }

        $.find(".project-visiblity-link").initProjectToggleLinks();

        

        //init auto complete textboxes
        $.find(".ul-autocomplete").ulAutoComplete();

        //init combobox.
        $.find(".ul-combobox").combobox();

        //init Dual List Box
        $.find(".ul-duallistbox").ulDualListBox();

        //register automatic value whitespace trimming event.
        $.find(".trim-on-blur").trimOn("blur");

        $.find(".toggle-control-on-change").toggleControlOn("change");

        $.find(".toggle-check-all-on-click").toggleCheckAllOn("click");

        $.find(".toggle-control-on-match").toggleControlOnMatch("change");
        $.find('.customer-lookup').customerlookupWidget();

      

        //$.find(".notifications").notification();
        // Dialog Bootstraps go here:
        $.find('#editUserTeamDialog').editUserTeamDialog();

        $.find('#editDocumentTemplateDialog').editDocumentTemplateDialog();

        $.find('.edit-task-dialog').taskEditDialog();

        $.find('#editTaskTypeDialog').editTaskTypeDialog();

        $.find('#project-template').projectTemplateDailog();

        $.find(".assign-modal").assignDialog();

        // End Dialog bootstraps

        //fill Parent height should be the last boot strap since all
        //other height adjustment need to be complete first.
        $.find(".status-marker").fillParentHeight();
        $.find('.editDocumentOnline').editDocumentOnline();

        $.find('#createTaskDocument').taskDocumentDialog();

        $.find('.ul-modal-dialog').ulDialog();
        $.find('.init-ajax-form').ulInitAjaxForm();
        $.find('#createProject').projectDialog();
    };

    //Page Load Init call at the window.document scope level.      
    UL.BootstrapControls(jquery(window.document));


}(jQuery));
