// Cursor light on the navbar pill's border.
// Writes the mouse position into CSS variables (--mx, --my, --glow) that navbar&footer.css reads.
document.addEventListener('DOMContentLoaded', function () {
  var pill = document.querySelector('.navbar .main-menu');
  if (!pill) {
    console.warn('navbar.js: .navbar .main-menu not found');
    return;
  }

  var FADE_DISTANCE = 300; // px from the pill where the light has fully faded out

  document.addEventListener('mousemove', function (e) {
    var r = pill.getBoundingClientRect();

    // distance from the cursor to the pill's rectangle (0 when inside it)
    var dx = Math.max(r.left - e.clientX, 0, e.clientX - r.right);
    var dy = Math.max(r.top - e.clientY, 0, e.clientY - r.bottom);
    var dist = Math.sqrt(dx * dx + dy * dy);

    pill.style.setProperty('--mx', (e.clientX - r.left) + 'px');
    pill.style.setProperty('--my', (e.clientY - r.top) + 'px');
    pill.style.setProperty('--glow', Math.max(0, 1 - dist / FADE_DISTANCE).toFixed(2));
  });

  document.documentElement.addEventListener('mouseleave', function () {
    pill.style.setProperty('--glow', '0');
  });
});