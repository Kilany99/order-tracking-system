const authService = {
    get token() {
        return localStorage.getItem("jwtToken");
    },
    isAuthenticated: () => !!localStorage.getItem("jwtToken")
};

export default authService;