#!/bin/bash

git clone https://github.com/supabase/storage .storage-server
cd .storage-server

git fetch --all --tags
git checkout tags/v1.0.10 -b v1.0.10-branch

cp .env.sample .env && cp .env.test.sample .env.test

npm install
npm run infra:restart
npm run build

cmd="npm run start";
eval "${cmd}" &>/dev/null & disown;