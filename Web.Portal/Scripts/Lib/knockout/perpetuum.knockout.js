/// <reference path="/@JSense.js" />


executeOnServer = function (model, url) {

    $.ajax({
        url: url,
        type: 'POST',
        data: ko.mapping.toJSON(model),
        dataType: "json",
        contentType: "application/json; charset=utf-8",
        success: function (data) {
            ko.mapping.fromJS(data, model);
            //alert(JSON.stringify(data));
        },
        error: function (error) {
            alert("There was an error posting the data to the server: " + error.responseText);
        }
    });

};




//executeOnServer = function (model, url) {

//    disableForm();

//    $.ajax({
//        url: url,
//        type: 'POST',
//        data: ko.mapping.toJSON(model),
//        dataType: "json",
//        contentType: "application/json; charset=utf-8",
//        success: function (data) {

//            //alert(data);
//            var newModel = ko.mapping.fromJSON(data);
//            ko.mapping.fromJS(newModel.Model, model);
//            var vr = ko.mapping.fromJS(newModel.ValidationResults);
//            clearValidationErrors();

//            if (!newModel.Successful()) {
//                if (vr != null && !vr.IsValid()) {
//                    notifyValidationErrors(vr);
//                }
//            }
//            else if(newModel.RedirectOnSuccess()) {
//                //successful && redirect
//                showSuccessMessage(newModel.Message());
//                window.setInterval(function () {
//                    window.location = newModel.RedirectUrl()  
//                },
//                newModel.RedirectDelay() + 400);
//            }

//            enableForm();

//        },
//        error: function (error) {
//            alert("There was an error posting the data to the server: " + error.responseText);
//            enableForm();
//        }
//    });

//};




//delayRedirect = function () {




//}

//showSuccessMessage = function (message) {

//    alert(message);

//}

//disableForm = function () {

//    //need to implement splash screen 
//    // that shows "loading..." and makes it
//   //not possible to mess with the form.

//}

//enableForm = function () {

//    //need to implement splash screen 
//    // that shows "loading..." and makes it
//    //not possible to mess with the form.

//}


//clearValidationErrors = function () {

//    var elements = jQuery(".input-validation-error");

//    if (elements != null) {
//        for (j = 0; j < elements.length; j++) {
//            $(elements[j]).removeClass("input-validation-error");
//        }
//    }

//}

//notifyValidationErrors = function (valResults) {

//    if (valResults != 'undefined' || valResults != null) {
//        if (!valResults.IsValid()) {

//            for (i = 0; i < valResults.Results().length; i++) {

//                var result = valResults.Results()[i];
//                var elements = jQuery("[data-bind*='" + result.PropertyName() + "'][data-bind*=showValidation]");

//                if (elements != null) {
//                    for (j = 0; j < elements.length; j++) {
//                        $(elements[j]).addClass("input-validation-error");
//                    }
//                }

//                //add tool tip
//                elements = jQuery("[data-bind*='" + result.PropertyName() + "'][data-bind*=showValToolTip]");

//                if (elements != null) {
//                    for (j = 0; j < elements.length; j++) {
//                        $(elements[j]).attr("title", result.Message());                        
//                    }
//                }

//                //test
//                /*  alert('MSG: ' + result.Message()
//                + '\nPropertyName: ' + result.PropertyName()
//                + '\nPropertyOwner: ' + result.PropertyOwner()); */
//            }
//        }
//    }
//}

//handleServerException = function (exception) {


//}
