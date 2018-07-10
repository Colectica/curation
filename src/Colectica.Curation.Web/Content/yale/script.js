(function ($) {

  Drupal.behaviors.jScrollPane = {
    attach: function (context, settings) {
      var jScrollPane = Drupal.settings.jScrollPane['class'];
      $(jScrollPane, context).jScrollPane();
    }
  };

})(jQuery);
