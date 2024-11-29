# Use an official Node.js runtime as a base image
FROM node:18-alpine

# Install Newman globally
RUN npm install -g newman@latest

# Set the working directory
WORKDIR /usr/src/app

# Copy the entire newman-reporter-influxdb directory
COPY newman-reporter-influxdb ./newman-reporter-influxdb

# Install and link the reporter
WORKDIR /usr/src/app/newman-reporter-influxdb
RUN npm install && \
    npm link

# Set working directory back to app root
WORKDIR /usr/src/app

# Set the entrypoint
ENTRYPOINT ["newman"]
