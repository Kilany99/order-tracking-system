class AuthService {
    constructor(baseUrl) {
      this.baseUrl = `${baseUrl}/api/auth`;
    }
      get token() {
    return localStorage.getItem('jwtToken');
  }

  isAuthenticated() {
    return !!this.token && !this.isTokenExpired();
  }

  isTokenExpired() {
    if (!this.token) return true;
    try {
      const payload = JSON.parse(atob(this.token.split('.')[1]));
      const exp = payload.exp * 1000; // Convert to milliseconds
      return Date.now() > exp;
    } catch {
      return true;
    }
  }

  logout() {
    localStorage.removeItem('jwtToken');
    window.location.href = '/auth/login';
  }
  
    async login(email, password) {
      try {
        const response = await $.ajax({
          url: `${this.baseUrl}/login`,
          type: 'POST',
          contentType: 'application/json',
          data: JSON.stringify({ email, password })
        });
        localStorage.setItem('jwtToken', response.token);
        window.location.href = '/'; // Redirect to index.html
      } catch (error) {
        // Check if error.responseJSON is defined and is an array
        let errorMessage = '';
        if (error.responseJSON && Array.isArray(error.responseJSON)) {
          errorMessage = error.responseJSON
            .map(err => `${err.code}: ${err.description}`)
            .join(' ');
        } else {
          errorMessage = 'Login failed. Check your credentials.';
        }
        showError(errorMessage);
      }
    }
  
    async register(fullName, email, password) {
      try {
        await $.ajax({
          url: `${this.baseUrl}/register`,
          type: 'POST',
          contentType: 'application/json',
          data: JSON.stringify({ fullName, email, password })
        });
        showMessage('Registration successful! Please login.'); 
      } catch (error) {
        let errorMessage = '';
        if (error.responseJSON && Array.isArray(error.responseJSON)) {
          errorMessage = error.responseJSON
            .map(err => `${err.code}: ${err.description}`)
            .join(' ');
        } else {
          errorMessage = 'Registration failed. Email may already exist.';
        }
        showError(errorMessage);
      }
    }
  }
  
  // Initialize AuthService .
  const authService = new AuthService('https://localhost:7094');
  
  // Form Submissions
  $('#loginForm').submit(async (e) => {
    e.preventDefault();
    await authService.login(
      $('#loginEmail').val(),
      $('#loginPassword').val()
    );
  });
  
  $('#registerForm').submit(async (e) => {
    e.preventDefault();
    await authService.register(
      $('#registerFullName').val(),
      $('#registerEmail').val(),
      $('#registerPassword').val()
    );
  });
  
  // Error Display Function
  function showError(message) {
    $('#errorMessage').text(message).fadeIn().delay(6000).fadeOut();
  }
  function showMessage(message){
    $('#successMessage').text(message).fadeIn().delay(6000).fadeOut();

  }
  
  