# Use an official Node.js runtime as the base image.
FROM node:16-alpine

# Set the working directory in the container.
WORKDIR /app

COPY package*.json ./

# Install dependencies.
RUN npm install

COPY . .

EXPOSE 3000

CMD ["npm", "start"]
