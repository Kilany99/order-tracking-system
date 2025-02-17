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
    });
  });