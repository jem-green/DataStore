@ECHO OFF
cls
del datastore.dbf
del datastore.idx
DatastoreConsole --filename "datastore" new
DatastoreConsole --filename "datastore" add --field "id" --type "int32"
DatastoreConsole --filename "datastore" add --field "name" --type "string" --length "10"
DatastoreConsole --filename "datastore" get --all "yes"
DatastoreConsole --filename "datastore" create --field "id" --value "0" --field "name" --value "hello"
DatastoreConsole --filename "datastore" create --field "id" --value "1" --field "name" --value "laura"
DatastoreConsole --filename "datastore" update --row "0" --field "id" --value "101" --field "name" --value "jeremy"
DatastoreConsole --filename "datastore" read --all "yes"
DatastoreConsole --filename "datastore" read --row "0"



 