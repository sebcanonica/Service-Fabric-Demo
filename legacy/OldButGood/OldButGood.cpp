// OldButGood.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"
#include "Windows.h"
#include <string>

#include <cpprest/http_client.h>
#include <cpprest/filestream.h>

#include <iostream>
#include <sstream>

using namespace web::http;
using namespace web::http::client;

int main()
{
    http_client client(L"http://localhost:19081");
    http_request request(methods::PUT);

    auto pid = GetCurrentProcessId();
    auto name = std::to_wstring(pid);
    auto partitionKey = std::to_wstring(pid % 10);
    auto uri = 
      L"SfApp/WebWithState/api/hello/" + name + L"?PartitionKey=" + partitionKey + L"&PartitionKind=Int64Range";
    std::wcout << "Saying hello to " << uri << std::endl;
    
    request.set_request_uri(uri);

    while (true) 
    {  
      try 
      {
        client.request(request).then([](http_response response)
        {
          std::wostringstream ss;
          ss << L"Server returned returned status code " << response.status_code() << L"." << std::endl;
          std::wcout << ss.str();
        }).wait();
        std::this_thread::sleep_for(std::chrono::seconds{ 3 });
      }
      catch (...) {
        std::wcout << L"Exception..." << std::endl;
      }
    }

    return 0;
}

