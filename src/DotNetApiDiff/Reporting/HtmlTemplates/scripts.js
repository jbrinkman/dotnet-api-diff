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

// Session storage helpers
function setSectionState(sectionId, state) {
    try {
        sessionStorage.setItem('section-' + sectionId, state);
    } catch (e) {
        // Ignore storage errors
    }
}

function getSectionState(sectionId) {
    try {
        return sessionStorage.getItem('section-' + sectionId);
    } catch (e) {
        return null;
    }
}

// New collapsible section functionality
function toggleSection(sectionId) {
    const section = document.getElementById(sectionId);
    const content = document.getElementById(sectionId + '-content');
    const button = section.querySelector('.section-toggle');
    const icon = button.querySelector('.toggle-icon');

    if (content.classList.contains('collapsed')) {
        // Expand section
        content.classList.remove('collapsed');
        button.classList.remove('collapsed');
        icon.textContent = '▶'; // Keep using ▶ and let CSS handle rotation
        setSectionState(sectionId, 'expanded');
    } else {
        // Collapse section
        content.classList.add('collapsed');
        button.classList.add('collapsed');
        icon.textContent = '▶'; // Keep using ▶ and let CSS handle rotation
        setSectionState(sectionId, 'collapsed');
    }
}

// Toggle type group functionality
function toggleTypeGroup(typeGroupId) {
    const content = document.getElementById(typeGroupId + '-content');
    const button = document.querySelector(`[onclick="toggleTypeGroup('${typeGroupId}')"] .type-toggle`);
    const icon = button?.querySelector('.toggle-icon');

    if (!content || !button || !icon) return;

    if (content.classList.contains('collapsed')) {
        // Expand type group
        content.classList.remove('collapsed');
        button.classList.remove('collapsed');
        icon.textContent = '▶'; // Keep using ▶ and let CSS handle rotation
        setTypeGroupState(typeGroupId, 'expanded');
    } else {
        // Collapse type group
        content.classList.add('collapsed');
        button.classList.add('collapsed');
        icon.textContent = '▶'; // Keep using ▶ and let CSS handle rotation
        setTypeGroupState(typeGroupId, 'collapsed');
    }
}

// Type group session storage helpers
function setTypeGroupState(typeGroupId, state) {
    try {
        sessionStorage.setItem('type-group-' + typeGroupId, state);
    } catch (e) {
        // Ignore storage errors
    }
}

function getTypeGroupState(typeGroupId) {
    try {
        return sessionStorage.getItem('type-group-' + typeGroupId);
    } catch (e) {
        return null;
    }
}

// Navigate to section with smooth scrolling and auto-expand
function navigateToSection(sectionId) {
    const section = document.getElementById(sectionId);
    const content = document.getElementById(sectionId + '-content');

    if (!section) return;

    // Auto-expand if collapsed
    if (content && content.classList.contains('collapsed')) {
        toggleSection(sectionId);
    }

    // Smooth scroll to section
    section.scrollIntoView({
        behavior: 'smooth',
        block: 'start'
    });

    // Brief highlight effect
    section.style.transition = 'box-shadow 0.3s ease';
    section.style.boxShadow = '0 0 20px rgba(0, 123, 255, 0.3)';
    setTimeout(() => {
        section.style.boxShadow = '';
    }, 1500);
}

// Initialize sections to collapsed state by default
function initializeSections() {
    const sections = ['breaking-changes', 'added-items', 'removed-items', 'modified-items'];

    sections.forEach(sectionId => {
        const section = document.getElementById(sectionId);
        const content = document.getElementById(sectionId + '-content');
        const button = section?.querySelector('.section-toggle');
        const icon = button?.querySelector('.toggle-icon');

        if (!section || !content || !button || !icon) return;

        // Check session storage first
        const savedState = getSectionState(sectionId);

        if (savedState === 'expanded') {
            // Expand based on saved state
            content.classList.remove('collapsed');
            button.classList.remove('collapsed');
            icon.textContent = '▶';
        } else {
            // Default to collapsed (including null/new sessions)
            content.classList.add('collapsed');
            button.classList.add('collapsed');
            icon.textContent = '▶';
            // Save the default state if not already set
            if (savedState === null) {
                setSectionState(sectionId, 'collapsed');
            }
        }
    });

    // Initialize type groups to collapsed state by default
    initializeTypeGroups();
}

// Initialize type groups to collapsed state by default
function initializeTypeGroups() {
    const typeGroups = document.querySelectorAll('.type-group');

    typeGroups.forEach(typeGroup => {
        const typeHeader = typeGroup.querySelector('.type-header');
        const typeContent = typeGroup.querySelector('.type-changes');
        const button = typeGroup.querySelector('.type-toggle');
        const icon = button?.querySelector('.toggle-icon');

        if (!typeHeader || !typeContent || !button || !icon) return;

        // Extract type group ID from onclick attribute
        const onclickAttr = typeHeader.getAttribute('onclick');
        const match = onclickAttr?.match(/toggleTypeGroup\('([^']+)'\)/);
        const typeGroupId = match ? match[1] : null;

        if (!typeGroupId) return;

        // Check session storage first
        const savedState = getTypeGroupState(typeGroupId);

        if (savedState === 'expanded') {
            // Expand based on saved state
            typeContent.classList.remove('collapsed');
            button.classList.remove('collapsed');
            icon.textContent = '▶';
        } else {
            // Default to collapsed (including null/new sessions)
            typeContent.classList.add('collapsed');
            button.classList.add('collapsed');
            icon.textContent = '▶';
            // Save the default state if not already set
            if (savedState === null) {
                setTypeGroupState(typeGroupId, 'collapsed');
            }
        }
    });
}

// Initialize sections when the page loads
document.addEventListener('DOMContentLoaded', initializeSections);
