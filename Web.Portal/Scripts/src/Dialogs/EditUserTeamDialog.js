
(function () {
	"use strict";
    $.fn.editUserTeamDialog = function () {
    	
        $(this).each(function (index, $elem) {
            var elem = $($elem);
            var options = elem.data();
            var addButton = elem.find(options.addButton);
            var userContainer = $('table.team-members tbody');
            var autoCompleteControl = $('#employeeLoginId');
            var includeTheirTeam = $('#include-their-team');
            var newrowTxt = function (index) {
            	var tablerow = $('#idnewrow').find('tbody').html();
            	return tablerow.replace(/newrow/g, index);
            };
            addButton.on("click", function (e) {
                e.preventDefault();
                var item = autoCompleteControl.ulAutoComplete('getSelectedItem');
	            if (item.Display === undefined) {
		            return;
	            }

	            var userList = $(userContainer).find('tr');
                var newrow = newrowTxt(userList.length);
                var newrowobject = $.parseHTML(newrow);
                $(newrowobject).find('.user-id').val(item.Id);
                $(newrowobject).find('.user-login-id').val(item.Display);
                $(newrowobject).find('.include-user-team').prop('checked', $(includeTheirTeam).prop('checked'));
                userContainer.append(newrowobject);

                autoCompleteControl.ulAutoComplete('clearSelectedItem');
            });
            
        });

        return $(this);
    };
}());

