function toggleSignature(detailsId) {
    const details = document.getElementById(detailsId);
    const button = details.previousElementSibling.querySelector('.toggle-btn');
    const icon = button.querySelector('.toggle-icon');

    if (details.style.display === 'none') {
        details.style.display = 'block';
        button.classList.add('expanded');
        button.innerHTML = '<span class="toggle-icon">▲</span> Hide Signature Details';
    } else {
        details.style.display = 'none';
        button.classList.remove('expanded');
        button.innerHTML = '<span class="toggle-icon">▼</span> View Signature Details';
    }
}

function toggleConfig() {
    const details = document.getElementById('config-details');
    const button = document.querySelector('.toggle-button');
    const icon = button.querySelector('.toggle-icon');
    const text = button.querySelector('.toggle-text');

    if (details.style.display === 'none') {
        details.style.display = 'block';
        icon.textContent = '▼';
        text.textContent = 'Hide Configuration Details';
    } else {
        details.style.display = 'none';
        icon.textContent = '▶';
        text.textContent = 'Show Configuration Details';
    }
}
