$DAYS=3650
$PASSWORD='nwebdav'
$OPENSSL_CMD="docker"
$OPENSSL_ARG=@("run","--rm","-v","$(Get-Location)/Certificates:/data","alpine/openssl")

New-Item -ItemType Directory -Force certificates

Write-Host "Generate CA"
& $OPENSSL_CMD ($OPENSSL_ARG+@("req","-x509","-nodes","-new","-sha256","-days","$DAYS","-newkey","rsa:2048","-keyout","data/RootCA.key","-out","data/RootCA.pem","-subj","/C=NL/CN=NWebDAV-Development-CA"))
& $OPENSSL_CMD ($OPENSSL_ARG+@("x509","-outform","pem","-in","data/RootCA.pem","-out","data/RootCA.crt"))

Write-Host "Generate localhost certificate"
Copy-Item domains.ext certificates
& $OPENSSL_CMD ($OPENSSL_ARG+@("req","-new","-nodes","-newkey","rsa:2048","-keyout","data/localhost.key","-out","data/localhost.csr","-subj","/C=NL/ST=Overijssel/L=Enschede/O=Localhost/CN=localhost"))
& $OPENSSL_CMD ($OPENSSL_ARG+@("x509","-req","-sha256","-days","$DAYS","-in","data/localhost.csr","-CA","data/RootCA.pem","-CAkey","data/RootCA.key","-CAcreateserial","-extfile","/data/domains.ext","-out","data/localhost.crt"))
& $OPENSSL_CMD ($OPENSSL_ARG+@("pkcs12","-export","-out","data/localhost.pfx","-inkey","data/localhost.key","-in","data/localhost.crt","-certfile","data/RootCA.crt","-passout","pass:$PASSWORD"))

Write-Host "Import certificate (run as administrator if this fails)"
Import-Certificate -FilePath certificates/RootCA.pem -CertStoreLocation Cert:\LocalMachine\Root
