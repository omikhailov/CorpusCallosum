#include <iostream>
#include <UserEnv.h>
#include <sddl.h>

#pragma comment(lib, "userenv.lib")

using namespace std;

int main()
{
    wstring containerName;

    PSID sid;

    LPSTR stringSid;

    LPWSTR path;

    ULONG pathLength;

    wcout << L"You can find SID for your UWP app in the Partner Center. If it is not yet published or you are going to use MSIX for deployment from website, FTP or shared folder, please enter your app container name. For UWP apps it is the same string as PFN (Package Family Name):\n\n";

    wcin >> containerName;

    DeriveAppContainerSidFromAppContainerName(containerName.c_str(), &sid);

    ConvertSidToStringSidA(sid, &stringSid);

    FreeSid(sid);

    wcout << L"SID: ";

    wcout << stringSid;

    wcout << L"\n";
}
