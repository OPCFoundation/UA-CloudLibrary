docker run --name my-postgres -e POSTGRES_PASSWORD=postgres -p 5432:5432 -v /c/Users/erichb/source/repos/UA-CloudLibrary/Tests/TestSetup/init-user-db.sql:/docker-entrypoint-initdb.d/init-user-db.sql -d postgres

