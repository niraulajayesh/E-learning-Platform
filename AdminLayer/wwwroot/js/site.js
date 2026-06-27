(() => {
  const ready = (callback) => {
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', callback);
    else callback();
  };

  ready(() => {
    document.querySelectorAll('table').forEach((table) => {
      if (!table.closest('.table-responsive')) {
        const wrapper = document.createElement('div');
        wrapper.className = 'table-responsive';
        table.parentNode.insertBefore(wrapper, table);
        wrapper.appendChild(table);
      }
    });

    document.querySelectorAll('form[data-confirm]').forEach((form) => {
      form.addEventListener('submit', (event) => {
        if (!window.confirm(form.dataset.confirm || 'Are you sure?')) event.preventDefault();
      });
    });

    document.querySelectorAll('.toast').forEach((toast) => {
      if (window.bootstrap) bootstrap.Toast.getOrCreateInstance(toast).show();
    });

    document.querySelectorAll('button:not([type])').forEach((button) => button.setAttribute('type', 'button'));
    document.querySelectorAll('a[target="_blank"]:not([rel])').forEach((link) => link.setAttribute('rel', 'noopener noreferrer'));
  });
})();
