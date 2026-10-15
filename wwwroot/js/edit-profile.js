// ===== PROFILE PAGE LOGIC =====
// (dropdown, logout and notification badge live in profile-dropdown.js)

// School database with branches
const schoolDatabase = {
    "STI": ["Alabang Campus", "Bacoor Campus", "Calamba Campus", "Caloocan Campus", "Cavite Campus", "Cubao Campus", "Dasmarinas Campus", "Fairview Campus", "Las Piñas Campus", "Lipa Campus", "Lucena Campus", "Makati Campus", "Manila Campus", "Novaliches Campus", "Ortigas Campus", "Quezon Avenue Campus", "Recto Campus", "Rosario Campus", "San Fernando Pampanga Campus", "Sta. Rosa Campus", "Taft Campus"],
};

// Rank thresholds (same XP ranges as Progress.html)
const ranks = [
    { min: 6500, name: 'Legend' },
    { min: 4500, name: 'Boss' },
    { min: 3000, name: 'Elite' },
    { min: 1800, name: 'Pro' },
    { min: 1200, name: 'Skilled' },
    { min: 700,  name: 'Rising' },
    { min: 300,  name: 'Beginner' },
    { min: 0,    name: 'Starter' }
];

// Load profile data
let currentAvatar = localStorage.getItem('profileAvatar') || '/avatars/Avatar 1.png';
let selectedAvatar = currentAvatar;
let userName = localStorage.getItem('profileName') || localStorage.getItem('userName') || 'User01';
let userSchool = localStorage.getItem('profileSchool') || 'Not set yet';
let userGrade = localStorage.getItem('profileGrade') || 'Not set yet';
let joinedDate = localStorage.getItem('joinedDate') || null;
let bestStreak = parseInt(localStorage.getItem('bestStreak')) || 0;
let badgeCount = parseInt(localStorage.getItem('badgeCount')) || 0;

// Load progress data
let streakDays = parseInt(localStorage.getItem('streakDays')) || 0;
let totalXP = parseInt(localStorage.getItem('totalXP')) || 0;

// Current rank name + icon from XP
function getCurrentRank() {
    const rank = ranks.find(function (r) { return totalXP >= r.min; });
    return {
        name: rank.name,
        iconPath: '/rank titles/' + rank.name.toLowerCase() + '.png'
    };
}

// "September 23, 2025" (also understands the older "23 Sep 2025" format)
function formatJoinedDate(value) {
    const date = new Date(value);
    if (isNaN(date)) return value;
    return date.toLocaleDateString('en-US', { month: 'long', day: 'numeric', year: 'numeric' });
}

function updateBestStreak() {
    if (streakDays > bestStreak) {
        bestStreak = streakDays;
        localStorage.setItem('bestStreak', bestStreak);
    }
}

function updateNavbarAvatar() {
    const navbarAvatarElement = document.getElementById('navbarAvatar');
    if (navbarAvatarElement) {
        navbarAvatarElement.src = currentAvatar;
    }
}

// Fill in everything on the profile view
function initializeProfile() {
    updateBestStreak();
    const rank = getCurrentRank();

    // First visit: remember the join date
    if (!joinedDate) {
        joinedDate = new Date().toISOString();
        localStorage.setItem('joinedDate', joinedDate);
    }

    document.getElementById('profileAvatar').querySelector('img').src = currentAvatar;
    document.getElementById('profileName').textContent = userName;
    document.getElementById('profileSchool').textContent = userSchool;
    document.getElementById('profileGrade').textContent = userGrade;
    document.getElementById('joinedDate').textContent = formatJoinedDate(joinedDate);

    document.getElementById('rankTitle').textContent = rank.name;
    document.getElementById('rankIconProfile').src = rank.iconPath;
    document.getElementById('badgeCount').textContent = badgeCount;
    document.getElementById('bestStreak').textContent = bestStreak + (bestStreak === 1 ? ' Day' : ' Days');

    updateNavbarAvatar();
}

// ===== Tabs (Stats / Feed) =====
function showTab(name) {
    document.querySelectorAll('.tab-btn').forEach(function (btn) {
        const isActive = btn.dataset.tab === name;
        btn.classList.toggle('active', isActive);
        btn.setAttribute('aria-selected', isActive);
    });
    document.getElementById('statsPanel').hidden = (name !== 'stats');
    document.getElementById('feedPanel').hidden = (name !== 'feed');
}

document.querySelectorAll('.tab-btn').forEach(function (btn) {
    btn.addEventListener('click', function () {
        showTab(btn.dataset.tab);
    });
});

// ===== Edit profile =====
function populateSchoolDropdown() {
    const schoolSelect = document.getElementById('schoolInput');
    schoolSelect.innerHTML = '<option value="">Select school</option>';

    Object.keys(schoolDatabase).sort().forEach(function (schoolName) {
        schoolDatabase[schoolName].forEach(function (campus) {
            const option = document.createElement('option');
            option.value = schoolName + ' - ' + campus;
            option.textContent = schoolName + ' - ' + campus;
            schoolSelect.appendChild(option);
        });
    });
}

function showEditView() {
    document.querySelector('.profile-view').classList.remove('active');
    document.querySelector('.edit-view').classList.add('active');

    populateSchoolDropdown();

    document.getElementById('nameInput').value = userName;
    document.getElementById('schoolInput').value = userSchool === 'Not set yet' ? '' : userSchool;
    document.getElementById('gradeInput').value = userGrade === 'Not set yet' ? '' : userGrade;
    document.getElementById('editAvatar').querySelector('img').src = currentAvatar;
}

function cancelEdit() {
    document.querySelector('.edit-view').classList.remove('active');
    document.querySelector('.profile-view').classList.add('active');
}

function saveProfile() {
    const name = document.getElementById('nameInput').value.trim();
    const school = document.getElementById('schoolInput').value;
    const grade = document.getElementById('gradeInput').value;

    if (name && school && grade) {
        userName = name;
        userSchool = school;
        userGrade = grade;

        localStorage.setItem('profileName', userName);
        localStorage.setItem('profileSchool', userSchool);
        localStorage.setItem('profileGrade', userGrade);
        localStorage.setItem('profileAvatar', currentAvatar);

        document.getElementById('profileName').textContent = userName;
        document.getElementById('profileSchool').textContent = userSchool;
        document.getElementById('profileGrade').textContent = userGrade;
        document.getElementById('profileAvatar').querySelector('img').src = currentAvatar;

        updateNavbarAvatar();
        cancelEdit();
    } else {
        alert('Please fill in all fields');
    }
}

// ===== Avatar modal =====
function showAvatarModal() {
    document.getElementById('avatarModal').classList.add('active');
    selectedAvatar = currentAvatar;
    updateAvatarSelection();
}

function closeAvatarModal() {
    document.getElementById('avatarModal').classList.remove('active');
}

function confirmAvatar() {
    currentAvatar = selectedAvatar;
    document.getElementById('editAvatar').querySelector('img').src = currentAvatar;

    // Save immediately and update every avatar on the page
    localStorage.setItem('profileAvatar', currentAvatar);
    document.getElementById('profileAvatar').querySelector('img').src = currentAvatar;
    updateNavbarAvatar();

    closeAvatarModal();
}

function updateAvatarSelection() {
    document.querySelectorAll('.avatar-option').forEach(function (option) {
        option.classList.toggle('selected', option.dataset.avatar === selectedAvatar);
    });
}

document.getElementById('avatarGrid').addEventListener('click', function (e) {
    const option = e.target.closest('.avatar-option');
    if (option) {
        selectedAvatar = option.dataset.avatar;
        updateAvatarSelection();
    }
});

// ===== Start =====
initializeProfile();

// Refresh when another tab (e.g. Progress) updates streak / XP / badges
window.addEventListener('storage', function (e) {
    if (e.key === 'streakDays' || e.key === 'totalXP' || e.key === 'badgeCount') {
        streakDays = parseInt(localStorage.getItem('streakDays')) || 0;
        totalXP = parseInt(localStorage.getItem('totalXP')) || 0;
        badgeCount = parseInt(localStorage.getItem('badgeCount')) || 0;
        initializeProfile();
    }
});