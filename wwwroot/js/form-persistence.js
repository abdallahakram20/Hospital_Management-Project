(function (window) {
    'use strict';

    const FormPersistence = {
        init(config) {
            const {
                storageKey,
                formSelector,
                fields,
                onRestore,
                debounceMs = 300
            } = config;

            const form = document.querySelector(formSelector);
            if (!form) return;

            const getElements = () =>
                fields
                    .map(id => document.getElementById(id))
                    .filter(Boolean);

            const getData = () => {
                const data = {};
                fields.forEach(id => {
                    const el = document.getElementById(id);
                    if (el) data[id] = el.value;
                });
                return data;
            };

            const hasAnyValue = data =>
                Object.values(data).some(v => v !== null && v !== undefined && String(v).trim() !== '');

            const save = () => {
                try {
                    const data = getData();
                    if (hasAnyValue(data)) {
                        localStorage.setItem(storageKey, JSON.stringify(data));
                    } else {
                        localStorage.removeItem(storageKey);
                    }
                } catch (e) {
                    console.warn('FormPersistence: could not save', e);
                }
            };

            const restore = () => {
                try {
                    const saved = localStorage.getItem(storageKey);
                    if (!saved) return false;

                    const data = JSON.parse(saved);
                    let restored = false;

                    fields.forEach(id => {
                        const el = document.getElementById(id);
                        if (el && data[id] !== undefined && data[id] !== '') {
                            el.value = data[id];
                            restored = true;
                        }
                    });

                    if (restored) {
                        if (typeof onRestore === 'function') onRestore(data);
                        showRestoreNotice(form);
                    }

                    return restored;
                } catch (e) {
                    console.warn('FormPersistence: could not restore', e);
                    return false;
                }
            };

            const clear = () => {
                try {
                    localStorage.removeItem(storageKey);
                } catch (e) {
                    console.warn('FormPersistence: could not clear', e);
                }
            };

            let debounceTimer;
            const debouncedSave = () => {
                clearTimeout(debounceTimer);
                debounceTimer = setTimeout(save, debounceMs);
            };

            getElements().forEach(el => {
                el.addEventListener('input', debouncedSave);
                el.addEventListener('change', debouncedSave);
            });

            form.addEventListener('submit', clear);

            restore();
        }
    };

    window.FormPersistence = FormPersistence;
})(window);
