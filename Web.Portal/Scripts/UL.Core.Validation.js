/// <reference path="_references.js" />

//(function ($) {
//    $.validator.unobtrusive.adapters.add("greaterdate", ["other"], function (options) {
//        alert(1);
//        var prefix = getModelPrefix(options.element.name),
//            other = options.params.other,
//            fullOtherName = appendModelPrefix(other, prefix),
//            element = $(options.form).find(":input[name='" + escapeAttributeValue(fullOtherName) + "']")[0];
//        alert();
//        setValidationValues(options, "greaterdate", element);
//    });

//    function getModelPrefix(fieldName) {
//        return fieldName.substr(0, fieldName.lastIndexOf(".") + 1);
//    }

//    function appendModelPrefix(value, prefix) {
//        if (value.indexOf("*.") === 0) {
//            value = value.replace("*.", prefix);
//        }
//        return value;
//    }

//    function setValidationValues(options, ruleName, value) {
//        options.rules[ruleName] = value;
//        if (options.message) {
//            options.messages[ruleName] = options.message;
//        }
//    }

//    function escapeAttributeValue(value) {
//        // As mentioned on http://api.jquery.com/category/selectors/
//        return value.replace(/([!"#$%&'()*+,./:;<=>?@\[\\\]^`{|}~])/g, "\\$1");
//    }


//    $.validator.addMethod('greaterdate',
//        function(val, element, param) {
//            alert();
//            var otherVal = $(param).val();

//            if (val && otherVal) {
//                if (Date.parse(val) < Date.parse(otherVal)) {
//                    return false;
//                }
//            }
//            return true;
//        });
//})(jQuery);


jQuery.validator.unobtrusive
      .adapters.addSingleVal("greaterdate", "other");

jQuery.validator.addMethod("greaterdate",
    function (val, element, other) {
        var modelPrefix = element.name.substr(
            0, element.name.lastIndexOf(".") + 1);
        var otherVal = $("[name=" + modelPrefix + other + "]").val();
        if (val && otherVal) {
            if (Date.parse(val) <= Date.parse(otherVal)) {
                return false;
            }
        }
        return true;
    }
);