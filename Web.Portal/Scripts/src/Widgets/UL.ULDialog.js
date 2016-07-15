(function($) {
	'use strict';
	UL.ULDialog = function(elem, options) {
		this.elem = elem;
		this.options = {};
		$.extend(this.options, options);
	};

	UL.ULDialog.prototype = {
		init: function() {
			this.elem.on('click', this.clickEventHandler());
		},
		clickEventHandler :function() {
			var self = this;

			return function(e, ui) {
				e.preventDefault();
				var options = new UL.ModalOptions();
				var callback = function (arg) {


					if (self.options.callback && typeof  self.options.callback === "function") {
						self.options.callback(arg, self.options);
					} else {
						if (arg.result === options.submitButtonText) {
							arg.modal.modal('hide');
							var proxy = new UL.Proxy();
							proxy.send({ id: self.options.id }, self.options.url,
								function (successful, vdata, error) {
									UL.Refresh();
								});
						}
					}
				};

				options.submitButtonText = self.options.submitButtonText;
				options.message = self.options.message;
				options.title = self.options.title;
				options.cancelButtonText = self.options.cancelButtonText;
				options.callback = callback;
				UL.ShowModal(options);
			};
		}
	};
	$.fn.ulDialog = function() {
		$(this).each(function(index, elem) {

			var $elem = $(elem);
			if ($elem.data('UL.ULDialog') === undefined) {
				
				var data = $elem.data();
				var component = new UL.ULDialog($elem, data);
				$elem.data("UL.ULDialog", component);
				component.init();
			}
		});
		return $(this);
	};

	UL.UnlockDocument = function (arg, options) {
		if (arg.result === options.submitButtonText) {
			arg.modal.modal('hide');
			var proxy = new UL.Proxy();
			proxy.send({ id: options.id }, options.url,
				function (successful, vdata, error) {
					UL.Refresh();
				});
		}

	};

}(jQuery));

