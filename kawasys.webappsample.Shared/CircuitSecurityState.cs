namespace kawasys.webappsample.Shared
{
    public class CircuitSecurityState
    {
        //preparation for propagating auth state, since WebApps run each connection in its own circuit, i dont need to reauth everything everytime i can just use the unique circuit id of the current session
        // since all remotes are in the shell, they can get the Auth Web Apps Circuit ID locally and then cross reference in the backend for its auth state
        public bool IsAuthorized { get; private set; }

        public void Authorize() => IsAuthorized = true;
    }
}
