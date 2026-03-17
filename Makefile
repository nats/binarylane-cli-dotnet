OPENSSL_VERSION := 3.0.13
OPENSSL_DIR := vendor/openssl-$(OPENSSL_VERSION)
OPENSSL_PREFIX := $(CURDIR)/vendor/musl-ssl
OPENSSL_TARBALL := vendor/openssl-$(OPENSSL_VERSION).tar.gz

PUBLISH_DIR := src/BinaryLane.Cli/bin/Release/net10.0/linux-musl-x64/publish
INSTALL_DIR := $(HOME)/.local/bin

.PHONY: generate build publish install test clean clean-openssl

generate:
	dotnet run --project src/BinaryLane.Cli.Generator -- openapi.json src/BinaryLane.Cli/Generated

build:
	dotnet build

# Build static OpenSSL with musl-gcc
# Kernel headers (linux/, asm/) are needed but not in musl include path;
# pass them via CFLAGS with -isystem to avoid glibc header conflicts.
MUSL_KERNEL_CFLAGS := -idirafter /usr/include -idirafter /usr/include/x86_64-linux-gnu

$(OPENSSL_PREFIX)/lib64/libssl.a: | $(OPENSSL_DIR)
	cd $(OPENSSL_DIR) && CC=musl-gcc CFLAGS="$(MUSL_KERNEL_CFLAGS)" ./Configure linux-x86_64 no-shared no-async no-tests --prefix=$(OPENSSL_PREFIX)
	$(MAKE) -C $(OPENSSL_DIR) -j$$(nproc) CNF_CFLAGS="$(MUSL_KERNEL_CFLAGS)"
	$(MAKE) -C $(OPENSSL_DIR) install_sw

$(OPENSSL_DIR): $(OPENSSL_TARBALL)
	tar xzf $(OPENSSL_TARBALL) -C vendor

$(OPENSSL_TARBALL):
	mkdir -p vendor
	curl -sL https://github.com/openssl/openssl/releases/download/openssl-$(OPENSSL_VERSION)/openssl-$(OPENSSL_VERSION).tar.gz -o $(OPENSSL_TARBALL)

openssl: $(OPENSSL_PREFIX)/lib64/libssl.a

publish: $(OPENSSL_PREFIX)/lib64/libssl.a
	OPENSSL_ROOT_DIR=$(OPENSSL_PREFIX) MUSL_SSL_LIB=$(OPENSSL_PREFIX)/lib64 dotnet publish src/BinaryLane.Cli -c Release "-p:CppCompilerAndLinker=$(CURDIR)/vendor/musl-gcc-wrapper"

install: publish
	mkdir -p $(INSTALL_DIR)
	cp $(PUBLISH_DIR)/bl $(INSTALL_DIR)/blnet

test: install
	dotnet test

clean:
	dotnet clean
	rm -f $(INSTALL_DIR)/blnet

clean-openssl:
	rm -rf vendor/openssl-* vendor/musl-ssl
