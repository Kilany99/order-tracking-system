const express = require('express');
const path = require('path');
const app = express();
app.use((req, res, next) => {
  const protectedRoutes = [
   // '/order/order-track',
    //'/order/new-order-form',
    //'/api/orders'
  ];

  if (protectedRoutes.some(route => req.path.startsWith(route))) {
    const token = req.headers.authorization?.split(' ')[1];
    
    if (!token) {
      return res.redirect('/auth/login');
    }

    try {
      const payload = JSON.parse(atob(token.split('.')[1]));
      if (payload.exp * 1000 < Date.now()) {
        return res.redirect('/auth/login');
      }
    } catch {
      return res.redirect('/auth/login');
    }
  }
  next();
});
// Serve static files
app.use(express.static(path.join(__dirname, 'public')));

// Routes
app.get('/', (req, res) => {
  res.sendFile(path.join(__dirname, 'public', 'index.html'));
});

app.get('/auth/login', (req, res) => {
  res.sendFile(path.join(__dirname, 'public', '/auth/login.html'));
});
app.get('/auth/register', (req, res) => {
  res.sendFile(path.join(__dirname, 'public', '/auth/register.html'));
});

app.get('/order/order-track', (req, res) => {
  res.sendFile(path.join(__dirname, 'public', '/order/order-track.html'));
});
app.get('/order/new-order-form', (req, res) => {
  res.sendFile(path.join(__dirname, 'public', '/order/new-order-form.html'));
});
app.listen(3000, '0.0.0.0', () => {
  console.log('Frontend running on http://localhost:3000');
});