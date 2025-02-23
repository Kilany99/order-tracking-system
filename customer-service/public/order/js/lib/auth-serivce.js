const authService = {
    get token() {
        return localStorage.getItem("jwtToken");
    },
    isAuthenticated: () => !!localStorage.getItem("jwtToken"),
    async secureFetch(url, options = {}) {
        // Check token expiration before any request
        if (this.isTokenExpired()) {
          this.logout();
          return Promise.reject('Session expired');
        }
      
        try {
          const response = await fetch(url, {
            ...options,
            headers: {
              ...options.headers,
              // Only add Authorization header for authenticated endpoints
              ...(url.includes('/api/') && { 
                'Authorization': `Bearer ${this.token}` 
              })
            }
          });
      
          // Handle 401 responses
          if (response.status === 401) {
            this.logout();
            return Promise.reject('Unauthorized');
          }
      
          return response;
        } catch (error) {
          if (error.message.includes('Unauthorized') || 
              error.message.includes('expired')) {
            this.logout();
          }
          throw error;
        }
      },
      isAuthenticated() {
        return !!this.token && !this.isTokenExpired();
      },
    
      isTokenExpired() {
        if (!this.token) return true;
        try {
          const payload = JSON.parse(atob(this.token.split('.')[1]));
          const exp = payload.exp * 1000; // Convert to milliseconds
          return Date.now() > exp;
        } catch {
          return true;
        }
      },
    
      logout() {
        localStorage.removeItem('jwtToken');
        window.location.href = '/auth/login';
      }
    
};

export default authService;