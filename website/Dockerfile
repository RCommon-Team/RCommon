# 07-10-24.2

#Modifying this will trigger deployment without a code change
FROM --platform=linux/amd64 node:18.14.2

ENV PORT=8080
ENV release_mode="production"
ENV BUILD_VERSION="3.0"

# Create app directory
WORKDIR /graphql-engine/docs


# A wildcard is used to ensure both package.json AND package-lock.json are copied
COPY package*.json ./

COPY yarn.lock ./

#RUN yarn install

# Copy needed files
COPY . .

RUN corepack enable && corepack prepare yarn@stable --activate && yarn set version 3.3.0 && yarn install

# Build static files
RUN yarn build

# Env vars are in the k8s manifest and ddn-docs secret

EXPOSE 8080

CMD ["yarn", "serve", "-p", "8080", "--host", "0.0.0.0"]

