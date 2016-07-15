/// <reference path="../_references.js" />
/// <reference path="../_ULReferences.js" />

(function () {
    'use strict';

    if (!window.UL) {
        window.UL = {};
    }

    UL.AssignDialog = function ($elem, options) {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="$elem" type="jQuery"></param>
        ///<field name="taskOwnerTbx" type="jQuery"></field>
        ///<field name="continueBtn" type="jQuery"></field>   

        this.elem = $elem;
        this.options = options;
        this.taskOwnerTbx = null;
        this.continueBtn = null;
    };

    UL.AssignDialog.prototype = {
        init: function () {

            if (this.options.currentUser === undefined) {
                throw 'Assign Dialog requires the current user\'s login as the data element "data-current-user"';
            }             

            this.taskOwnerTbx = this.elem.find("input[type='text']");
            this.continueBtn = this.elem.find(".continue-button");
            this.elem.find('input:radio[name=TaskOwnerAssigned]').change(this.getRadioChangedHandler());
            this.taskOwnerTbx.blur(this.getTextboxBlurHandler());
            this.continueBtn.click(this.getContinueButtonClickHandler());
            this.elem.on('hidden.bs.modal', this.getOnModelCloseHandler());
        },
        getContinueButtonClickHandler: function () {
            return function (e, ui) {
                //do nothing internally event is handled externally
                return;
            };
        },
        getOnModelCloseHandler: function () {
            var self = this;
            return function (e, args) {
                self.resetSelection();
            };
        },
        getRadioChangedHandler: function () {

            return function (e, args) {
                if ($(this).val() === "AssignToHandler") {
                    $('input[name =TaskOwner]').prop('disabled', false);
                } else {
                    $('input[name =TaskOwner]').val('');
                    $('input[name =TaskOwner]').prop('disabled', true);
                }
            };

        },
        getTextboxBlurHandler: function () {
           
            return function (e, args) {
                var ctrl = $(this);
                ctrl.val(ctrl.val().trim());
            };
        },
        getSelectTaskOwner: function () {

            var selectedRadio = this.elem.find('input:radio[name=TaskOwnerAssigned]:checked');
            if (selectedRadio.val() === "AssignToHandler") {
                return this.taskOwnerTbx.val();
            }

            return this.options.currentUser;
        },
        close: function () {
            this.elem.modal('hide');
            this.resetSelection();
        },
        open: function () {
            this.elem.modal('show');
        },
        resetSelection: function () {
            var radio = this.elem.find("#radio1");
            radio.check();
            radio.trigger("change");

        }
    };

    $.fn.assignDialog = function () {

        $(this).each(function (index, elem) {
            var $elem = $(elem);

            if ($elem.data("UL.AssignDialog") === undefined) {
                var data = $elem.data();
                var component = new UL.AssignDialog($elem, data);
                $elem.data("UL.AssignDialog", component);
                component.init();
            }

        });

    };

}());