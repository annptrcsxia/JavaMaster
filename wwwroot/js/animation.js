
// Scroll animation
const observerOptions = {
    threshold: 0.1,
    rootMargin: '0px 0px -100px 0px'
};

const observer = new IntersectionObserver((entries) => {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            entry.target.classList.add('animate-in');
        }
    });
}, observerOptions);

// Observe all elements with scroll-animate class
document.querySelectorAll('.scroll-animate, .scroll-animate-delay').forEach(el => {
    observer.observe(el);
});


function toggleFAQ(button) {
    const faqItem = button.parentElement;
    const wasActive = faqItem.classList.contains('active');
    
    // Close all FAQ items
    const allItems = document.querySelectorAll('.faq-item');
    allItems.forEach(item => {
        item.classList.remove('active');
    });
    
    // Toggle current item (if it wasn't already active)
    if (!wasActive) {
        faqItem.classList.add('active');
    }
}