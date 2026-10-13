FROM nginx:alpine

COPY nginx.conf /etc/nginx/conf.d/default.conf

COPY . /usr/share/nginx/html/

# Bersihkan file yang tidak perlu di-serve
RUN rm -f /usr/share/nginx/html/Dockerfile \
    && rm -f /usr/share/nginx/html/nginx.conf

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]