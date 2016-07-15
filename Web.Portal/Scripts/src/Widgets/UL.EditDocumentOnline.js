(function () {
	'use strict';

	$.fn.editDocumentOnline = function () {
		$(this).each(function (index, element) {

			$(element).on('click', function (e) {
				e.preventDefault();
				var elem = $(element);
				var data = elem.data();
				var proxyOptions = new UL.ProxyOptions();
				//proxyOptions.blockedElement = elem;
				var proxy = new UL.Proxy(proxyOptions);
				proxy.send({ id: data.id }, data.href,
					function (successful, vdata, error) {
						if (vdata.Success) {
							if (data.openNewWindow) {
								window.open(data.documentHref, "Preview Document");
							} else {
								window.location.assign(data.documentHref);
								setTimeout(function () { UL.Refresh(); }, 5000);
							}
						} else {
							UL.ShowAlert("Edit Document Online", vdata.Message);
						}
					});
			});


		});
	};
}());