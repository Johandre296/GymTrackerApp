window.gymTrackerSortable = {
    instances: {},

    init: function (key, element, dotNetRef, disabled) {
        if (typeof Sortable === 'undefined' || !element) {
            return;
        }

        this.destroy(key);

        this.instances[key] = Sortable.create(element, {
            animation: 150,
            disabled: disabled,
            handle: '.drag-handle',
            onEnd: function (evt) {
                if (evt.oldIndex !== evt.newIndex) {
                    dotNetRef.invokeMethodAsync('OnReorder', evt.oldIndex, evt.newIndex);
                }
            }
        });
    },

    setDisabled: function (key, disabled) {
        var instance = this.instances[key];
        if (instance) {
            instance.option('disabled', disabled);
        }
    },

    destroy: function (key) {
        var instance = this.instances[key];
        if (instance) {
            instance.destroy();
            delete this.instances[key];
        }
    }
};
