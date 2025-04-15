# 使用 openresty/openresty:alpine 镜像
FROM openresty/openresty:alpine

# 添加 community 仓库
RUN echo "http://dl-cdn.alpinelinux.org/alpine/edge/community" >> /etc/apk/repositories

# 安装必要的构建工具和 lua-cjson
RUN apk update && \
    apk add --no-cache build-base lua5.1 lua5.1-dev wget && \
    wget https://luarocks.org/releases/luarocks-3.9.2.tar.gz && \
    tar zxpf luarocks-3.9.2.tar.gz && \
    cd luarocks-3.9.2 && \
    ./configure --lua-version=5.1 && \
    make && \
    make install && \
    cd .. && \
    rm -rf luarocks-3.9.2* && \
    luarocks install lua-cjson && \
    apk del build-base lua5.1-dev wget

# 复制 Nginx 配置文件
COPY ./nginx/conf.d /etc/nginx/conf.d

# 启动 Nginx
CMD ["openresty", "-g", "daemon off;"]