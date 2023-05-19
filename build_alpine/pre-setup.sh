# gen certs if not present
if ! ((test -e /etc/ssl/private/nginx-selfsigned.key) && (test -e /etc/ssl/certs/nginx-selfsigned.crt)); then
    echo "Self-signed x509 sertificate files not detected. Generating..."
    openssl req -x509 -nodes -days 36500 -newkey rsa:2048 -subj "/CN=US/C=US/L=San Fransisco" -keyout /etc/ssl/private/nginx-selfsigned.key -out /etc/ssl/certs/nginx-selfsigned.crt
fi

# check allowed ips is valid
if (testvar='all' && [[ $VDB_ALLOWED_IP = $testvar ]]); then
    unset testvar;
elif !(ipcalc -n "${VDB_ALLOWED_IP}"); then
    echo "Incorrect value of VDB_ALLOWED_IP environment variable was ignored."
    echo "VDB_ALLOWED_IP was set to ALL."
    VDB_ALLOWED_IP="all";
fi

# gen white list files. There is a typo "while"->"white" (no need to fix actually)
if ! test -e "/etc/nginx/snippets/while_list.conf"; then
    echo "Nginx while_list configuration file not detected. Generating..."
    echo "allow ${VDB_ALLOWED_IP}; deny all;" > /etc/nginx/snippets/while_list.conf
fi

# check if there is sign key generation enabled
if test "$VDB_GENERATE_JWT_SIG" = "true"; then
    if ! test -e "/run/secrets/generated_sig.json"; then
        echo "Generating random 512 bits long JWT signing key into /run/secrets/generated_sig.json..."
        echo "{\"GeneratedSigningKey\":{\"SigningKeyBase64\":\"$(head -c 64 /dev/random | base64 -w 0)\"}}" > /run/secrets/generated_sig.json
    fi
fi


echo "Spinning up the Nginx reverse-proxy..."
nginx
echo "Spinning up the ASP WebAPI..."
dotnet /app/main_server_api.dll --no-launch-profile

tail -f /dev/null