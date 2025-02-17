const express = require('express');
const path = require('path');
const app = express();

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

app.listen(3000, () => {
  console.log('Frontend running on http://localhost:3000');
});