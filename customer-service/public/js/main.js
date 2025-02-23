// Redirect to auth.html if not authenticated
$(document).ready(() => {
    const token = localStorage.getItem('jwtToken');
    if (!token) {
      window.location.href = 'auth/auth.html';
    }
  
    // Logout Button
    $('#logout').click(() => {
      localStorage.removeItem('jwtToken');
      window.location.href = 'auth/auth.html';
      // Session timeout handling
    let inactivityTimer;

    function resetTimer() {
      clearTimeout(inactivityTimer);
      inactivityTimer = setTimeout(() => {
        if (authService.isAuthenticated()) {
          authService.logout();
        }
      }, 300000); // 5 minutes inactivity timeout
    }

    // Track user activity
    document.addEventListener('mousemove', resetTimer);
    document.addEventListener('keypress', resetTimer);
    document.addEventListener('click', resetTimer);

    // Initial check on page load
    document.addEventListener('DOMContentLoaded', () => {
      if (!authService.isAuthenticated() && window.location.pathname !== '/auth/login') {
        window.location.href = '/auth/login';
      }
    });
    });
  });