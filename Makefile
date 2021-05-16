all: build

build:
	./quickbuild.sh \
	 	-j2 \
		--abbreviate-paths \
		--link=static \
		--runtime-link=shared \
		variant=release \
		libraries \
	| tee log.txt

clean:
	./clean.sh && rm log.txt
