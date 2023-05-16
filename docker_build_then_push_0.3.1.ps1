docker build . -t "luminodiode/vdb_main_server_api:0.3.1-beta" -t "luminodiode/vdb_main_server_api:latest";
docker push "luminodiode/vdb_main_server_api:0.3.1-beta"; docker push "luminodiode/vdb_main_server_api:latest";
pause "Press any key to exit";